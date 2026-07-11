using System;
using System.IO;
using Core.Constants;
using Core.Services;
using Events.Camera;
using Events.UI;
using ShipFactory.Serialization;
using ShipFactory.UI.Views.ShipLibrary;
using Ships;
using UI;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;

namespace ShipFactory
{
    [DefaultExecutionOrder(-100)]
    public class ShipFactoryController : PanelRendererBase
    {
        private const string CanvasContainerName = "canvas-container";
        private const string SnapshotExtension = ".json";
        private const string DefaultShipName = "Ship";
        private static readonly object ModuleDragPointerBlocker = new();
        private static readonly object UiHoverBlocker = new();
        private static readonly object UiPointerDownBlocker = new();

        [Header("Controllers")]
        [SerializeField] private ShipLibraryController libraryController;

        [SerializeField]
        private ShipModuleCatalog shipModuleCatalog;

        [SerializeField] private string snapshotFolderName = "ShipSnapshots";
        [SerializeField] private CameraResetRequestEventChannel cameraResetRequestEventChannel;

        [SerializeField] private Ship initialShip;
        private Camera _camera;
        private VisualElement _canvasContainer;

        private ShipFactoryCanvasController _canvasController;

        [Inject]
        private IGameInput _gameInput;

        private GameObject _gameObjectUnderPointer;
        private bool _isModuleDragBlockingCamera;
        private bool _isPaused;
        private bool _isUiHoverBlockingCamera;
        private bool _isUiPointerDownBlockingCamera;

        private Button _loadShipButton;
        private ModulePaletteController _paletteController;

        private VisualElement _pauseOverlay;
        private VisualElement _pauseOverlayHost;

        [Inject]
        private PointerOverUiEventChannel _pointerOverUiChannel;

        private Button _quitButton;
        private Button _resumeButton;
        private VisualElement _root;
        private Button _saveShipButton;
        private Button _settingsButton;
        private SettingsPanelController _settingsPanelController;

        private TextField _shipNameField;

        [Inject]
        private IShipSnapshotService _snapshotService;

        [Inject]
        private IInstantiator _instantiator;

        [Inject]
        private TextInputFocusEventChannel _textInputFocusChannel;

        private TextInputFocusTracker _textInputFocusTracker;

        protected override void Awake()
        {
            base.Awake();
            _camera = Camera.main;

            if (libraryController == null)
                throw new UnityException("[ShipFactoryController] ShipLibraryController is required.");

            if (shipModuleCatalog == null)
                throw new UnityException($"[ShipFactoryController] {nameof(ShipModuleCatalog)} is not assigned!");

            if (initialShip != null)
                initialShip.IsDesignMode = true;
        }

        private void Update()
        {
            SetModuleDragPointerBlock(_canvasController?.IsDraggingModule == true);
            _canvasController?.RefreshShipResourcesPanel();
            _canvasController?.RefreshCameraInfoPanel(_camera);
            HandlePauseInput();
            HandleRotationInput();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetPaused(false);
        }

        protected override void BindUiCore(
            VisualElement root)
        {
            if (cameraResetRequestEventChannel == null)
                throw new InvalidOperationException(
                    "[ShipFactoryController] CameraResetRequestEventChannel is not assigned!");

            _root = root;
            _canvasController = new ShipFactoryCanvasController(
                root, _gameInput, _instantiator, shipModuleCatalog, cameraResetRequestEventChannel);
            _paletteController = new ModulePaletteController(root, shipModuleCatalog);

            _shipNameField = root.Q<TextField>("ship-name-field");
            _saveShipButton = root.Q<Button>("save-ship-button");
            _loadShipButton = root.Q<Button>("load-ship-button");

            if (_shipNameField == null || _saveShipButton == null)
                throw new InvalidOperationException("[ShipFactoryController] Save controls are missing in UXML.");

            _paletteController.OnModuleDragStarted += OnModuleDragStarted;
            _paletteController.OnModuleDragFinished += OnModuleDragFinished;
            _canvasController.OnModuleDragFinished += OnModuleDragFinished;
            _canvasController.OnInputLockChanged += OnCanvasInputLockChanged;
            _paletteController.OnModuleHoverStarted += OnPaletteModuleHoverStarted;
            _paletteController.OnModuleHoverEnded += OnPaletteModuleHoverEnded;
            _saveShipButton.clicked += SaveSnapshot;
            _loadShipButton.clicked += ShowSnapshotLibrary;

            var initialName = initialShip != null ? initialShip.name : DefaultShipName;
            _shipNameField.value = initialName;

            if (initialShip != null)
                _canvasController.SetShip(initialShip);

            BindPauseUi(root);
            RegisterCameraDragPointerBlockers(root);
            _textInputFocusTracker = new TextInputFocusTracker(_textInputFocusChannel);
            _textInputFocusTracker.Track(_shipNameField);
        }

        private void ShowSnapshotLibrary()
        {
            var snapshotFolderPath = Path.Combine(Application.persistentDataPath, snapshotFolderName);

            libraryController.CloseClicked -= HideSnapshotLibrary;
            libraryController.SnapshotSelected -= LoadSnapshotFromLibrary;
            libraryController.CloseClicked += HideSnapshotLibrary;
            libraryController.SnapshotSelected += LoadSnapshotFromLibrary;
            libraryController.Show(snapshotFolderPath);
        }

        private void HideSnapshotLibrary()
        {
            libraryController.gameObject.SetActive(false);
            libraryController.CloseClicked -= HideSnapshotLibrary;
            libraryController.SnapshotSelected -= LoadSnapshotFromLibrary;
        }

        private void LoadSnapshotFromLibrary(
            string snapshotPath)
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
                _shipNameField.value = string.IsNullOrWhiteSpace(snapshot.shipName)
                    ? DefaultShipName
                    : snapshot.shipName;
                _canvasController.RebuildShipModules();
                HideSnapshotLibrary();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[ShipFactoryController] Failed to load snapshot '{snapshotPath}'.\n{exception}",
                    this);
                _canvasController.ShowErrorMessage("Failed to load the selected ship snapshot.");
            }
        }

        protected override void UnbindUiCore()
        {
            SetModuleDragPointerBlock(false);
            SetUiHoverBlock(false);
            SetUiPointerDownBlock(false);
            ReleaseGameObjectPointerBlocking();

            UnbindPauseUi();
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
            }

            if (_canvasController != null)
            {
                _canvasController.OnModuleDragFinished -= OnModuleDragFinished;
                _canvasController.OnInputLockChanged -= OnCanvasInputLockChanged;
                _canvasController.Dispose();
            }

            _textInputFocusTracker?.Release(_shipNameField);
            _textInputFocusTracker = null;

            _canvasContainer = null;
            _canvasController = null;
            _paletteController = null;
            _shipNameField = null;
            _saveShipButton = null;
            _root = null;
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

            var snapshotFolderPath = Path.Combine(Application.persistentDataPath, snapshotFolderName);
            Directory.CreateDirectory(snapshotFolderPath);

            var existingNamePredicate = new Func<string, bool>(candidateName =>
            {
                var candidateFileName = SnapshotNameUtility.SanitizeFileName(candidateName) + SnapshotExtension;
                var candidatePath = Path.Combine(snapshotFolderPath, candidateFileName);
                return File.Exists(candidatePath);
            });

            if (existingNamePredicate(requestedName))
            {
                var suggestedCopyName = SnapshotNameUtility.GetNextCopyName(requestedName, existingNamePredicate);
                _shipNameField.value = suggestedCopyName;
                _canvasController.ShowWarningMessage(
                    $"'{requestedName}' already exists. Suggested copy name: '{suggestedCopyName}'.");
                return;
            }

            var snapshot = _snapshotService.CaptureSnapshot(initialShip);
            if (snapshot == null)
            {
                _canvasController.ShowErrorMessage("Failed to capture ship snapshot.");
                return;
            }

            snapshot.shipName = requestedName;
            var json = JsonUtility.ToJson(snapshot, true);

            var sanitizedName = SnapshotNameUtility.SanitizeFileName(requestedName);
            var outputPath = Path.Combine(snapshotFolderPath, sanitizedName + SnapshotExtension);

            File.WriteAllText(outputPath, json);

            _canvasController.ShowInfoMessage($"Saved snapshot '{requestedName}'.");
            Debug.Log($"[ShipFactoryController] Snapshot saved to: {outputPath}");
        }

        private void OnCanvasInputLockChanged(bool isLocked)
        {
            _paletteController.SetInputLocked(isLocked);
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

        private void BindPauseUi(VisualElement root)
        {
            _pauseOverlay = root.Q<VisualElement>(SharedUiElementNames.Pause.Overlay);
            _pauseOverlayHost = root.Q<VisualElement>(SharedUiElementNames.Pause.OverlayHost);
            var title = root.Q<Label>(SharedUiElementNames.Pause.Title);
            _resumeButton = root.Q<Button>(SharedUiElementNames.Pause.ResumeButton);
            _settingsButton = root.Q<Button>(SharedUiElementNames.Pause.SettingsButton);
            _quitButton = root.Q<Button>(SharedUiElementNames.Pause.QuitButton);

            if (title == null || _resumeButton == null || _settingsButton == null || _quitButton == null ||
                _pauseOverlay == null)
                throw new InvalidOperationException(
                    "[ShipFactoryController] Pause elements missing in ShipFactory UXML.");

            title.text = "Ship Factory Paused";
            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = DisplayStyle.None;
            _pauseOverlay.style.display = DisplayStyle.None;

            _resumeButton.clicked += OnResumeClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _quitButton.clicked += QuitToMainMenu;
            _settingsPanelController = new SettingsPanelController(root, false);
        }

        private void UnbindPauseUi()
        {
            _settingsPanelController?.Unbind();
            _settingsPanelController = null;

            if (_resumeButton != null)
                _resumeButton.clicked -= OnResumeClicked;
            if (_settingsButton != null)
                _settingsButton.clicked -= OnSettingsClicked;
            if (_quitButton != null)
                _quitButton.clicked -= QuitToMainMenu;

            _pauseOverlay = null;
            _pauseOverlayHost = null;
            _resumeButton = null;
            _settingsButton = null;
            _quitButton = null;
        }

        private void OnResumeClicked()
        {
            SetPaused(false);
        }

        private void OnSettingsClicked()
        {
            _settingsPanelController?.Toggle();
        }

        private void HandleRotationInput()
        {
            if (_isPaused || _canvasController == null) return;
            if (_gameInput.IsTextInputFocused) return;
            if (!Input.GetKeyDown(KeyCode.R)) return;

            var counterClockwise = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            _canvasController.RotateActiveModule(counterClockwise ? -90 : 90);
        }

        private void HandlePauseInput()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
                return;

            if (_settingsPanelController != null && _settingsPanelController.IsOpen)
            {
                _settingsPanelController.Hide();
                return;
            }

            SetPaused(!_isPaused);
        }

        private void SetPaused(bool paused)
        {
            if (_isPaused == paused)
                return;

            _isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;
            if (_pauseOverlay != null)
                _pauseOverlay.style.display = paused ? DisplayStyle.Flex : DisplayStyle.None;

            _canvasController?.SetExternalInputLock(paused);
            _paletteController?.SetInputLocked(paused);

            if (!paused && _settingsPanelController != null && _settingsPanelController.IsOpen)
                _settingsPanelController.Hide();
        }

        private static void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneNames.MainMenu);
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