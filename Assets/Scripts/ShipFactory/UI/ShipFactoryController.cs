using System;
using System.IO;
using Core.Constants;
using Core.Gameplay.Sound;
using Core.Services;
using Core.ShipFactory;
using Events.Game;
using Events.UI;
using ShipFactory.UI.Runtime;
using ShipFactory.UI.Views.ShipLibrary;
using Ships;
using UI.Common;
using UI.Components.OptionsPopup;
using UI.Stack;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory.UI
{
    [DefaultExecutionOrder(-100)]
    public class ShipFactoryController : PanelRendererBase
    {
        private const string CanvasContainerName = "canvas-container";
        private const string DefaultShipName = "Ship";
        private const string ClearShipConfirmOptionId = "confirm";
        private const string InfoAcknowledgeOptionId = "ok";
        private static readonly object ModuleDragPointerBlocker = new();
        private static readonly object UiHoverBlocker = new();
        private static readonly object UiPointerDownBlocker = new();

        [SerializeField] private DesignShip initialShip;
        [SerializeField] private ShipFactoryFeedback feedback;
        private Camera _camera;
        private VisualElement _canvasContainer;

        private ShipFactoryCanvasController _canvasController;

        [Inject]
        private ShipFactoryCanvasController.Factory _canvasControllerFactory;

        private (bool needToShow, string shipName) _duplicateShipNameWarning;

        [Inject]
        private IGameInput _gameInput;

        private GameObject _gameObjectUnderPointer;

        [Inject]
        private IGameUi _gameUi;

        private bool _isModuleDragBlockingCamera;
        private bool _isUiHoverBlockingCamera;
        private bool _isUiPointerDownBlockingCamera;

        private Button _loadShipButton;
        private ModulePaletteController _paletteController;

        [Inject]
        private ModulePaletteController.Factory _paletteControllerFactory;

        [Inject]
        private PauseStateEventChannel _pauseStateChannel;

        [Inject]
        private PointerOverUiEventChannel _pointerOverUiChannel;

        private VisualElement _root;
        private Button _saveShipButton;

        private TextField _shipNameField;

        [Inject] private IShipSnapshotRepository _shipSnapshotRepository;

        [Inject]
        private IShipSnapshotService _snapshotService;

        [Inject(Optional = true)]
        private ISoundManager _soundManager;

        [Inject]
        private TextInputFocusEventChannel _textInputFocusChannel;

        private TextInputFocusTracker _textInputFocusTracker;

        protected override void Awake()
        {
            base.Awake();
            _camera = Camera.main;

            if (_canvasControllerFactory == null)
                throw new UnityException(
                    "[ShipFactoryController] ShipFactoryCanvasController.Factory is required.");

            if (_paletteControllerFactory == null)
                throw new UnityException(
                    "[ShipFactoryController] ModulePaletteController.Factory is required.");

            if (_gameUi == null)
                throw new UnityException("[ShipFactoryController] IGameUi is required.");

            if (_pauseStateChannel == null)
                throw new UnityException("[ShipFactoryController] PauseStateEventChannel is required.");
        }

        private void Start()
        {
            _gameUi.SetRoot(this);
        }

        private void Update()
        {
            SetModuleDragPointerBlock(_canvasController?.IsDraggingModule == true);
            _canvasController?.RefreshShipResourcesPanel();
            _canvasController?.RefreshCameraInfoPanel(_camera);
            HandleRotationInput();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _pauseStateChannel.Register(OnPauseChanged);
        }

        protected override void OnDisable()
        {
            OnPauseChanged(false);
            _pauseStateChannel.Unregister(OnPauseChanged);
            base.OnDisable();
        }

        protected override void BindUiCore(
            VisualElement root)
        {
            _root = root;

            _canvasController = _canvasControllerFactory.Create(root, feedback);
            _paletteController = _paletteControllerFactory.Create(root);

            _shipNameField = root.Q<TextField>("ship-name-field");
            _saveShipButton = root.Q<Button>("save-ship-button");
            _loadShipButton = root.Q<Button>("load-ship-button");

            if (_shipNameField == null || _saveShipButton == null || _loadShipButton == null)
                throw new InvalidOperationException("[ShipFactoryController] Save controls are missing in UXML.");

            _paletteController.OnModuleDragStarted += OnModuleDragStarted;
            _paletteController.OnModuleDragFinished += OnModuleDragFinished;
            _canvasController.OnModuleDragFinished += OnModuleDragFinished;
            _canvasController.OnInputLockChanged += OnCanvasInputLockChanged;
            _canvasController.OnShipCompositionChanged += OnShipCompositionChanged;
            _canvasController.OnClearShipRequested += OnClearShipRequested;
            _paletteController.OnModuleHoverStarted += OnPaletteModuleHoverStarted;
            _paletteController.OnModuleHoverEnded += OnPaletteModuleHoverEnded;
            _paletteController.OnBlockedPlacementClicked += OnBlockedPlacementClicked;
            _saveShipButton.clicked += SaveSnapshot;
            _loadShipButton.clicked += ShowSnapshotLibrary;

            var initialName = initialShip ? initialShip.name : DefaultShipName;
            _shipNameField.value = initialName;

            _shipNameField.RegisterValueChangedCallback(OnShipNameChanged);

            if (initialShip)
                _canvasController.SetShip(initialShip);
            else
                SyncPaletteToShipState();

            RegisterCameraDragPointerBlockers(root);
            _textInputFocusTracker = new TextInputFocusTracker(_textInputFocusChannel);
            _textInputFocusTracker.Track(_shipNameField);
        }

        private void OnShipNameChanged(ChangeEvent<string> evt)
        {
            _duplicateShipNameWarning = (true, evt.newValue);
        }

        private void ShowSnapshotLibrary()
        {
            var library = _gameUi.PushById<ShipLibraryController>(UIPanelPrefabConstants.ShipLibrary);
            library.SnapshotSelected += LoadSnapshotFromLibrary;
            library.SnapshotDeleted += OnSnapshotDeleted;
        }

        private void LoadSnapshotFromLibrary(string snapshotPath)
        {
            if (initialShip == null) throw new InvalidOperationException("No ship assigned to ShipFactory.");

            try
            {
                var snapshot = _snapshotService.LoadSnapshotFromFile(snapshotPath);
                if (snapshot == null)
                {
                    _canvasController.ShowErrorMessage("Failed to load the selected ship snapshot.");
                    return;
                }

                _snapshotService.ApplySnapshot(initialShip, snapshot);
                initialShip.InitializeModules();
                _shipNameField.SetValueWithoutNotify(string.IsNullOrWhiteSpace(snapshot.shipName)
                    ? DefaultShipName
                    : snapshot.shipName);
                _canvasController.RebuildShipModules();

                _duplicateShipNameWarning = (false, snapshot.shipName);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[ShipFactoryController] Failed to load snapshot '{snapshotPath}'.\n{exception}",
                    this);
                _canvasController.ShowErrorMessage("Failed to load the selected ship snapshot.");
            }
        }

        private void OnSnapshotDeleted(string snapshotPath)
        {
            _canvasController.ShowInfoMessage($"Deleted snapshot '{Path.GetFileNameWithoutExtension(snapshotPath)}'.");
        }

        protected override void UnbindUiCore()
        {
            SetModuleDragPointerBlock(false);
            SetUiHoverBlock(false);
            SetUiPointerDownBlock(false);
            ReleaseGameObjectPointerBlocking();

            UnregisterCameraDragPointerBlockers(_root);

            if (_saveShipButton != null)
                _saveShipButton.clicked -= SaveSnapshot;
            if (_loadShipButton != null)
                _loadShipButton.clicked -= ShowSnapshotLibrary;

            if (_paletteController != null)
            {
                _paletteController.OnModuleDragStarted -= OnModuleDragStarted;
                _paletteController.OnModuleDragFinished -= OnModuleDragFinished;
                _paletteController.OnModuleHoverStarted -= OnPaletteModuleHoverStarted;
                _paletteController.OnModuleHoverEnded -= OnPaletteModuleHoverEnded;
                _paletteController.OnBlockedPlacementClicked -= OnBlockedPlacementClicked;
            }

            if (_canvasController != null)
            {
                _canvasController.OnModuleDragFinished -= OnModuleDragFinished;
                _canvasController.OnInputLockChanged -= OnCanvasInputLockChanged;
                _canvasController.OnShipCompositionChanged -= OnShipCompositionChanged;
                _canvasController.OnClearShipRequested -= OnClearShipRequested;
                _canvasController.Dispose();
            }

            _shipNameField.UnregisterValueChangedCallback(OnShipNameChanged);

            _textInputFocusTracker?.Release(_shipNameField);
        }

        private void SaveSnapshot()
        {
            if (initialShip == null)
            {
                _canvasController.ShowErrorMessage("No ship assigned to ShipFactory.");
                return;
            }

            var requestedName = string.IsNullOrWhiteSpace(_shipNameField.value)
                ? DefaultShipName
                : _shipNameField.value.Trim();

            if (_shipSnapshotRepository.SnapshotExists(requestedName)
                && (_duplicateShipNameWarning.needToShow || _duplicateShipNameWarning.shipName != requestedName))
            {
                _canvasController.ShowWarningMessage("This ships already exists! Select save again to overwrite it.");
                _duplicateShipNameWarning = (false, requestedName);
                return;
            }

            var snapshot = _snapshotService.CaptureSnapshot(initialShip);
            if (snapshot == null)
            {
                _canvasController.ShowErrorMessage("Failed to capture ship snapshot.");
                return;
            }

            snapshot.shipName = requestedName;

            _shipSnapshotRepository.SaveSnapshot(snapshot);

            _canvasController.ShowInfoMessage($"Saved snapshot '{requestedName}'.");
            Debug.Log("[ShipFactoryController] Snapshot saved.");
        }

        private void OnCanvasInputLockChanged(bool isLocked)
        {
            _paletteController.SetInputLocked(isLocked);
        }

        private void OnShipCompositionChanged()
        {
            SyncPaletteToShipState();
        }

        private void SyncPaletteToShipState()
        {
            _paletteController.SyncToShipState(
                _canvasController.ShipHasModules,
                _canvasController.ShipHasCommandModule);
        }

        private void OnModuleDragFinished()
        {
            _paletteController.FinishModuleDrag();
            _canvasController.RefreshShipResourcesPanel();
        }

        private void OnModuleDragStarted(ShipModuleSO shipModuleSO, Vector2 startPointerPosition)
        {
            if (_canvasController.IsInputLocked)
            {
                _paletteController.FinishModuleDrag();
                return;
            }

            _canvasController.BeginModuleDrop(shipModuleSO);
        }

        private void OnPaletteModuleHoverStarted(ShipModuleSO moduleSO)
        {
            _canvasController.ShowPaletteModuleInfo(moduleSO);
        }

        private void OnPaletteModuleHoverEnded(ShipModuleSO moduleSO)
        {
            _canvasController.HidePaletteModuleInfo(moduleSO);
        }

        private void OnClearShipRequested()
        {
            _gameUi.ShowOptions(
                "Clear ship?",
                "Deleting the command module removes the entire ship. This cannot be undone.",
                OnClearShipPopupOptionSelected,
                new OptionsPopupOption("cancel", "Cancel", OptionsPopupOptionStyle.Ghost),
                new OptionsPopupOption(ClearShipConfirmOptionId, "Clear Ship", OptionsPopupOptionStyle.Danger));
        }

        private void OnBlockedPlacementClicked(string title, string description)
        {
            _gameUi.ShowOptions(
                title,
                description,
                null,
                new OptionsPopupOption(InfoAcknowledgeOptionId, "OK", OptionsPopupOptionStyle.Primary));
        }

        private void OnClearShipPopupOptionSelected(string optionId)
        {
            if (optionId != ClearShipConfirmOptionId)
                return;

            _canvasController.ClearShip();
        }

        private void OnPauseChanged(bool paused)
        {
            _canvasController?.SetExternalInputLock(paused);
            _paletteController?.SetInputLocked(paused);
        }

        private void HandleRotationInput()
        {
            if (_gameInput.IsPaused || _canvasController == null) return;
            if (_gameInput.IsTextInputFocused) return;
            if (!Input.GetKeyDown(KeyCode.R)) return;

            var counterClockwise = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _canvasController.RotateActiveModule(counterClockwise ? -90 : 90);
        }

        private void RegisterCameraDragPointerBlockers(VisualElement root)
        {
            _canvasContainer = root.Q<VisualElement>(CanvasContainerName);
            if (_canvasContainer == null)
                throw new InvalidOperationException(
                    "[ShipFactoryController] canvas-container not found in UXML.");

            root.RegisterCallback<PointerDownEvent>(OnPointerDownForCameraBlock, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUpForCameraBlock, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMoveForUiHoverBlock, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(OnPointerLeaveForUiHoverBlock, TrickleDown.TrickleDown);
        }

        private void UnregisterCameraDragPointerBlockers(VisualElement root)
        {
            if (root == null)
                return;

            root.UnregisterCallback<PointerDownEvent>(OnPointerDownForCameraBlock);
            root.UnregisterCallback<PointerUpEvent>(OnPointerUpForCameraBlock);
            root.UnregisterCallback<PointerMoveEvent>(OnPointerMoveForUiHoverBlock);
            root.UnregisterCallback<PointerLeaveEvent>(OnPointerLeaveForUiHoverBlock);
        }

        private void OnPointerDownForCameraBlock(PointerDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (evt.target is not VisualElement target)
                return;

            if (!IsUnderCanvasContainer(target))
            {
                SetUiPointerDownBlock(true);
                return;
            }

            var objectUnderPointer = _gameInput.ObjectUnderPointer;
            if (objectUnderPointer == null)
                return;

            _gameObjectUnderPointer = objectUnderPointer;
            _pointerOverUiChannel.Raise(new PointerOverUiData(_gameObjectUnderPointer, true));
        }

        private void OnPointerUpForCameraBlock(PointerUpEvent evt)
        {
            if (evt.button != 0)
                return;

            SetUiPointerDownBlock(false);
            ReleaseGameObjectPointerBlocking();
        }

        private bool IsUnderCanvasContainer(VisualElement element)
        {
            while (element != null)
            {
                if (element == _canvasContainer)
                    return true;

                element = element.parent;
            }

            return false;
        }

        private void OnPointerMoveForUiHoverBlock(PointerMoveEvent evt)
        {
            if (evt.target is not VisualElement target)
                return;

            SetUiHoverBlock(!IsUnderCanvasContainer(target));
        }

        private void OnPointerLeaveForUiHoverBlock(PointerLeaveEvent evt)
        {
            SetUiHoverBlock(false);
        }

        private void SetUiHoverBlock(bool isBlocking)
        {
            if (_isUiHoverBlockingCamera == isBlocking)
                return;

            _isUiHoverBlockingCamera = isBlocking;
            _pointerOverUiChannel.Raise(new PointerOverUiData(UiHoverBlocker, isBlocking));
        }

        private void SetUiPointerDownBlock(bool isBlocking)
        {
            if (_isUiPointerDownBlockingCamera == isBlocking)
                return;

            _isUiPointerDownBlockingCamera = isBlocking;
            _pointerOverUiChannel.Raise(new PointerOverUiData(UiPointerDownBlocker, isBlocking));
        }

        private void ReleaseGameObjectPointerBlocking()
        {
            if (_gameObjectUnderPointer == null)
                return;

            _pointerOverUiChannel.Raise(new PointerOverUiData(_gameObjectUnderPointer, false));
            _gameObjectUnderPointer = null;
        }

        private void SetModuleDragPointerBlock(bool isBlocking)
        {
            if (_isModuleDragBlockingCamera == isBlocking)
                return;

            _isModuleDragBlockingCamera = isBlocking;
            _pointerOverUiChannel.Raise(new PointerOverUiData(ModuleDragPointerBlocker, isBlocking));
        }
    }
}