using System;
using System.Collections.Generic;
using System.IO;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Main
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        private const string SnapshotFolderName = "ShipSnapshots";
        private const string SelectedSnapshotNameKey = "selectedShipSnapshotName";
        private const string SelectedSnapshotFileKey = "selectedShipSnapshotFile";
        private const string ShipFactorySceneName = "ShipFactory";
        private const string FallbackCombatSceneName = "MainGame";
        private const string LegacyCombatSceneName = "Main";

        private readonly List<string> _snapshotDisplayNames = new();
        private readonly List<string> _snapshotFilePaths = new();
        private Button _quitButton;
        private Button _settingsButton;
        private SettingsPanelController _settingsPanelController;
        private DropdownField _shipDropdown;
        private Button _shipFactoryButton;
        private Button _shipSelectCancelButton;
        private VisualElement _shipSelectionOverlay;
        private Button _shipSelectLaunchButton;
        private Button _startButton;

        private UIDocument _uiDocument;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindMainMenuUi();
        }

        private void BindMainMenuUi()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null)
                throw new InvalidOperationException("[MainMenuController] RootVisualElement is missing.");

            _startButton = root.Q<Button>("start-button");
            _shipFactoryButton = root.Q<Button>("ship-factory-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("quit-button");
            _shipSelectionOverlay = root.Q<VisualElement>("ship-select-overlay");
            _shipDropdown = root.Q<DropdownField>("ship-dropdown");
            _shipSelectCancelButton = root.Q<Button>("ship-select-cancel-button");
            _shipSelectLaunchButton = root.Q<Button>("ship-select-launch-button");

            if (_startButton == null || _shipFactoryButton == null || _settingsButton == null || _quitButton == null ||
                _shipSelectionOverlay == null || _shipDropdown == null || _shipSelectCancelButton == null ||
                _shipSelectLaunchButton == null)
                throw new InvalidOperationException(
                    "[MainMenuController] Required UI elements are missing in template.");

            _startButton.clicked += OpenShipSelectionDialog;
            _shipFactoryButton.clicked += OpenShipFactory;
            _settingsButton.clicked += OpenSettings;
            _quitButton.clicked += QuitGame;
            _shipSelectCancelButton.clicked += CloseShipSelectionDialog;
            _shipSelectLaunchButton.clicked += LaunchCombat;

            _settingsPanelController = new SettingsPanelController(root, true);
        }

        private void OpenShipSelectionDialog()
        {
            RefreshShipSnapshots();
            _shipSelectionOverlay.style.display = DisplayStyle.Flex;
        }

        private void CloseShipSelectionDialog()
        {
            _shipSelectionOverlay.style.display = DisplayStyle.None;
        }

        private static void OpenShipFactory()
        {
            SceneManager.LoadScene(ShipFactorySceneName);
        }

        private void OpenSettings()
        {
            _settingsPanelController.Toggle();
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LaunchCombat()
        {
            var selectedIndex = Mathf.Max(0, _snapshotDisplayNames.IndexOf(_shipDropdown.value));
            var selectedName = _snapshotDisplayNames.Count > 0 ? _snapshotDisplayNames[selectedIndex] : "Default Ship";
            var selectedFile = _snapshotFilePaths.Count > selectedIndex
                ? _snapshotFilePaths[selectedIndex]
                : string.Empty;

            PlayerPrefs.SetString(SelectedSnapshotNameKey, selectedName);
            PlayerPrefs.SetString(SelectedSnapshotFileKey, selectedFile);
            PlayerPrefs.Save();

            CloseShipSelectionDialog();
            SceneManager.LoadScene(ResolveCombatSceneName());
        }

        private static string ResolveCombatSceneName()
        {
            return Application.CanStreamedLevelBeLoaded(LegacyCombatSceneName)
                ? LegacyCombatSceneName
                : FallbackCombatSceneName;
        }

        private void RefreshShipSnapshots()
        {
            _snapshotDisplayNames.Clear();
            _snapshotFilePaths.Clear();

            var snapshotDirectory = Path.Combine(Application.persistentDataPath, SnapshotFolderName);
            if (Directory.Exists(snapshotDirectory))
                foreach (var filePath in Directory.GetFiles(snapshotDirectory, "*.json"))
                {
                    _snapshotDisplayNames.Add(Path.GetFileNameWithoutExtension(filePath));
                    _snapshotFilePaths.Add(filePath);
                }

            if (_snapshotDisplayNames.Count == 0)
            {
                _snapshotDisplayNames.Add("Default Ship");
                _snapshotFilePaths.Add(string.Empty);
            }

            _shipDropdown.choices = _snapshotDisplayNames;
            _shipDropdown.index = 0;
        }
    }
}