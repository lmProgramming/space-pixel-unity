using System;
using System.IO;
using ShipFactory.Serialization;
using Ships;
using Ships.Serialization;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ShipFactory
{
    [RequireComponent(typeof(UIDocument))]
    public class ShipFactoryController : MonoBehaviour
    {
        private const string SnapshotExtension = ".json";
        private const string DefaultShipName = "Ship";
        private const string MainMenuSceneName = "MainMenu";

        [SerializeField] private ModulePrefabLibrary modulePrefabLibrary;
        [SerializeField] private Ship initialShip;

        [SerializeField] private string snapshotFolderName = "ShipSnapshots";

        private ShipFactoryCanvasController _canvasController;
        private bool _isPaused;
        private ModulePaletteController _paletteController;
        private VisualElement _pauseOverlay;
        private VisualElement _pauseOverlayHost;
        private bool _pauseUiInitialized;
        private Button _saveShipButton;
        private SettingsPanelController _settingsPanelController;

        private TextField _shipNameField;
        private UIDocument _uiDocument;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            if (modulePrefabLibrary == null)
                Debug.LogError("[ShipFactoryController] ModulePrefabLibrary is not assigned!", this);
        }

        private void Update()
        {
            _canvasController?.RefreshShipResourcesPanel();
            HandlePauseInput();
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null)
                throw new InvalidOperationException("[ShipFactoryController] UI root is missing.");

            _canvasController = new ShipFactoryCanvasController(root);
            _paletteController = new ModulePaletteController(root, modulePrefabLibrary);

            _shipNameField = root.Q<TextField>("ship-name-field");
            _saveShipButton = root.Q<Button>("save-ship-button");

            if (_shipNameField == null || _saveShipButton == null)
                throw new InvalidOperationException("[ShipFactoryController] Save controls are missing in UXML.");

            _paletteController.OnModuleDragStarted += OnModuleDragStarted;
            _paletteController.OnModuleDragFinished += OnModuleDragFinished;
            _canvasController.OnModuleDragFinished += OnModuleDragFinished;
            _canvasController.OnInputLockChanged += OnCanvasInputLockChanged;
            _paletteController.OnModuleHoverStarted += OnPaletteModuleHoverStarted;
            _paletteController.OnModuleHoverEnded += OnPaletteModuleHoverEnded;
            _saveShipButton.clicked += SaveSnapshot;

            var initialName = initialShip != null ? initialShip.name : DefaultShipName;
            _shipNameField.value = initialName;

            if (initialShip != null)
                _canvasController.SetShip(initialShip);

            InitializePauseUi(root);
        }

        private void OnDisable()
        {
            if (_saveShipButton != null)
                _saveShipButton.clicked -= SaveSnapshot;

            if (_paletteController == null || _canvasController == null)
                return;

            _paletteController.OnModuleDragStarted -= OnModuleDragStarted;
            _paletteController.OnModuleDragFinished -= OnModuleDragFinished;
            _canvasController.OnModuleDragFinished -= OnModuleDragFinished;
            _canvasController.OnInputLockChanged -= OnCanvasInputLockChanged;
            _paletteController.OnModuleHoverStarted -= OnPaletteModuleHoverStarted;
            _paletteController.OnModuleHoverEnded -= OnPaletteModuleHoverEnded;

            SetPaused(false);
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

            var snapshotService = new ShipSnapshotService();
            var snapshot = snapshotService.CaptureSnapshot(initialShip);
            if (snapshot == null)
            {
                _canvasController.ShowErrorMessage("Failed to capture ship snapshot.");
                return;
            }

            snapshot.shipName = requestedName;
            var json = snapshotService.ToJson(snapshot);

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

            _canvasController.BeginModuleDrop(shipModuleSO, startPointerPosition);
        }

        private void OnPaletteModuleHoverStarted(ShipModuleSO moduleSO)
        {
            _canvasController.ShowPaletteModuleInfo(moduleSO);
        }

        private void OnPaletteModuleHoverEnded(ShipModuleSO moduleSO)
        {
            _canvasController.HidePaletteModuleInfo(moduleSO);
        }

        private void InitializePauseUi(VisualElement root)
        {
            if (_pauseUiInitialized)
                return;

            _pauseOverlay = root.Q<VisualElement>("pause-overlay");
            _pauseOverlayHost = root.Q<VisualElement>("pause-overlay-host");
            var title = root.Q<Label>("pause-title");
            var resumeButton = root.Q<Button>("pause-resume-button");
            var settingsButton = root.Q<Button>("pause-settings-button");
            var quitButton = root.Q<Button>("pause-quit-button");

            if (title == null || resumeButton == null || settingsButton == null || quitButton == null)
                throw new InvalidOperationException(
                    "[ShipFactoryController] Pause elements missing in ShipFactory UXML.");

            title.text = "Ship Factory Paused";
            if (_pauseOverlayHost != null)
                _pauseOverlayHost.style.display = DisplayStyle.None;
            _pauseOverlay.style.display = DisplayStyle.None;
            resumeButton.clicked += () => { SetPaused(false); };
            settingsButton.clicked += () => { _settingsPanelController.Toggle(); };
            quitButton.clicked += QuitToMainMenu;
            _settingsPanelController = new SettingsPanelController(root, false);
            _pauseUiInitialized = true;
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

        private void QuitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuSceneName);
        }
    }
}