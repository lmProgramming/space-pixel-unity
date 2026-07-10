using System;
using System.Collections.Generic;
using System.IO;
using Core.Constants;
using Core.State;
using UI.Common;
using UI.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.MainMenu
{
    [RequireComponent(typeof(PanelRenderer))]
    public class MainMenuController : MonoBehaviour
    {
        private const string SnapshotFolderName = "ShipSnapshots";
        private const float DefaultAsteroidCount = 6f;
        private const float DefaultEnemyShipCount = 0f;
        private const float DefaultFriendlyShipCount = 0f;

        private readonly List<string> _snapshotDisplayNames = new();
        private readonly List<string> _snapshotFilePaths = new();
        private Slider _asteroidCountSlider;
        private Slider _enemyCountSlider;
        private Slider _friendlyCountSlider;

        private bool _isBound;
        private PanelRenderer _panelRenderer;
        private Button _quitButton;
        private Button _settingsButton;
        private SettingsPanelController _settingsPanelController;
        private DropdownField _shipDropdown;
        private Button _shipFactoryButton;
        private Button _shipSelectCancelButton;
        private VisualElement _shipSelectionOverlay;
        private Button _shipSelectLaunchButton;
        private Button _startButton;
        private int _uiVersion = -1;

        private void Awake()
        {
            _panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnEnable()
        {
            _panelRenderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            _panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            UnbindMainMenuUi();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            if (version == _uiVersion && _isBound)
                return;

            if (version != _uiVersion)
                UnbindMainMenuUi();

            _uiVersion = version;
            BindMainMenuUi(root);
        }

        private void BindMainMenuUi(VisualElement root)
        {
            if (_isBound || root == null)
                return;

            _startButton = root.Q<Button>("start-button");
            _shipFactoryButton = root.Q<Button>("ship-factory-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("quit-button");
            _shipSelectionOverlay = root.Q<VisualElement>("ship-select-overlay");
            _shipDropdown = root.Q<DropdownField>("ship-dropdown");
            _shipSelectCancelButton = root.Q<Button>("ship-select-cancel-button");
            _shipSelectLaunchButton = root.Q<Button>("ship-select-launch-button");
            _asteroidCountSlider = root.Q<Slider>("asteroid-count-slider");
            _enemyCountSlider = root.Q<Slider>("enemy-count-slider");
            _friendlyCountSlider = root.Q<Slider>("friendly-count-slider");

            if (_startButton == null || _shipFactoryButton == null || _settingsButton == null || _quitButton == null ||
                _shipSelectionOverlay == null || _shipDropdown == null || _shipSelectCancelButton == null ||
                _shipSelectLaunchButton == null || _asteroidCountSlider == null || _enemyCountSlider == null ||
                _friendlyCountSlider == null)
                throw new InvalidOperationException(
                    "[MainMenuController] Required UI elements are missing in template.");

            _startButton.clicked += OpenShipSelectionDialog;
            _shipFactoryButton.clicked += OpenShipFactory;
            _settingsButton.clicked += OpenSettings;
            _quitButton.clicked += QuitGame;
            _shipSelectCancelButton.clicked += CloseShipSelectionDialog;
            _shipSelectLaunchButton.clicked += LaunchCombat;
            ConfigureCountSlider(_asteroidCountSlider, DefaultAsteroidCount, OnAsteroidCountSliderChanged);
            ConfigureCountSlider(_enemyCountSlider, DefaultEnemyShipCount, OnEnemyCountSliderChanged);
            ConfigureCountSlider(_friendlyCountSlider, DefaultFriendlyShipCount, OnFriendlyCountSliderChanged);

            DesignSystemThemeService.RegisterVisualTree(root);
            _settingsPanelController = new SettingsPanelController(root, true);
            _isBound = true;
        }

        private void UnbindMainMenuUi()
        {
            if (!_isBound)
                return;

            _settingsPanelController?.Unbind();
            _settingsPanelController = null;

            _startButton.clicked -= OpenShipSelectionDialog;
            _shipFactoryButton.clicked -= OpenShipFactory;
            _settingsButton.clicked -= OpenSettings;
            _quitButton.clicked -= QuitGame;
            _shipSelectCancelButton.clicked -= CloseShipSelectionDialog;
            _shipSelectLaunchButton.clicked -= LaunchCombat;
            _asteroidCountSlider.UnregisterValueChangedCallback(OnAsteroidCountSliderChanged);
            _enemyCountSlider.UnregisterValueChangedCallback(OnEnemyCountSliderChanged);
            _friendlyCountSlider.UnregisterValueChangedCallback(OnFriendlyCountSliderChanged);

            _startButton = null;
            _shipFactoryButton = null;
            _settingsButton = null;
            _quitButton = null;
            _shipSelectionOverlay = null;
            _shipDropdown = null;
            _shipSelectCancelButton = null;
            _shipSelectLaunchButton = null;
            _asteroidCountSlider = null;
            _enemyCountSlider = null;
            _friendlyCountSlider = null;
            _isBound = false;
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
            SceneManager.LoadScene(SceneNames.ShipFactory);
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
            if (_snapshotFilePaths.Count == 0)
            {
                Debug.LogError($"[{nameof(MainMenuController)}] No ship snapshots found!");
                return;
            }

            var selectedIndex = Mathf.Max(0, _snapshotDisplayNames.IndexOf(_shipDropdown.value));
            var selectedName = _snapshotDisplayNames[selectedIndex];
            var selectedFile = _snapshotFilePaths[selectedIndex];

            SaveState.PlayerShipName = selectedName;
            SaveState.PlayerShipSnapshotFilePath = selectedFile;
            SaveState.AsteroidCount = Mathf.RoundToInt(_asteroidCountSlider.value);
            SaveState.EnemyShipCount = Mathf.RoundToInt(_enemyCountSlider.value);
            SaveState.FriendlyShipCount = Mathf.RoundToInt(_friendlyCountSlider.value);

            SceneManager.LoadScene(SceneNames.MainGame);
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

        private static void ConfigureCountSlider(Slider slider, float defaultValue,
            EventCallback<ChangeEvent<float>> onValueChanged)
        {
            slider.SetValueWithoutNotify(defaultValue);
            slider.RegisterValueChangedCallback(onValueChanged);
        }

        private void OnAsteroidCountSliderChanged(ChangeEvent<float> evt)
        {
            _asteroidCountSlider.SetValueWithoutNotify(Mathf.Round(evt.newValue));
        }

        private void OnEnemyCountSliderChanged(ChangeEvent<float> evt)
        {
            _enemyCountSlider.SetValueWithoutNotify(Mathf.Round(evt.newValue));
        }

        private void OnFriendlyCountSliderChanged(ChangeEvent<float> evt)
        {
            _friendlyCountSlider.SetValueWithoutNotify(Mathf.Round(evt.newValue));
        }
    }
}