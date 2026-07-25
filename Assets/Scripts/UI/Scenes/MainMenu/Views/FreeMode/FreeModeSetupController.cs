using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Gameplay;
using Core.Services;
using Core.Ships;
using Core.State;
using Core.UI;
using UI.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Scenes.MainMenu.Views.FreeMode
{
    public class FreeModeSetupController : PanelRendererBase
    {
        private const float DefaultAsteroidCount = 6f;
        private const float DefaultEnemyShipCount = 0f;
        private const float DefaultFriendlyShipCount = 0f;

        private readonly List<string> _snapshotDisplayNames = new();
        private readonly List<string> _snapshotFilePaths = new();

        private Slider _asteroidCountSlider;
        private Button _cancelButton;
        private Slider _enemyCountSlider;
        private Slider _friendlyCountSlider;
        private Button _launchButton;
        private DropdownField _shipDropdown;

        [Inject] private IShipSnapshotRepository _snapshotRepository;

        protected override void BindUiCore(VisualElement root)
        {
            _shipDropdown = root.Q<DropdownField>("ship-dropdown");
            _cancelButton = root.Q<Button>("ship-select-cancel-button");
            _launchButton = root.Q<Button>("ship-select-launch-button");
            _asteroidCountSlider = root.Q<Slider>("asteroid-count-slider");
            _enemyCountSlider = root.Q<Slider>("enemy-count-slider");
            _friendlyCountSlider = root.Q<Slider>("friendly-count-slider");

            if (_shipDropdown == null || _cancelButton == null || _launchButton == null ||
                _asteroidCountSlider == null || _enemyCountSlider == null || _friendlyCountSlider == null)
                throw new InvalidOperationException(
                    "[FreeModeSetupController] Required UI elements are missing in template.");

            _cancelButton.clicked += OnCancelClicked;
            _launchButton.clicked += LaunchCombat;
            ConfigureCountSlider(_asteroidCountSlider, DefaultAsteroidCount, OnAsteroidCountSliderChanged);
            ConfigureCountSlider(_enemyCountSlider, DefaultEnemyShipCount, OnEnemyCountSliderChanged);
            ConfigureCountSlider(_friendlyCountSlider, DefaultFriendlyShipCount, OnFriendlyCountSliderChanged);

            _snapshotRepository.Model.Changed += OnSnapshotCatalogChanged;
            RefreshShipSnapshots();
        }

        protected override void UnbindUiCore()
        {
            if (_cancelButton != null)
                _cancelButton.clicked -= OnCancelClicked;
            if (_launchButton != null)
                _launchButton.clicked -= LaunchCombat;

            _snapshotRepository.Model.Changed -= OnSnapshotCatalogChanged;
            _asteroidCountSlider?.UnregisterValueChangedCallback(OnAsteroidCountSliderChanged);
            _enemyCountSlider?.UnregisterValueChangedCallback(OnEnemyCountSliderChanged);
            _friendlyCountSlider?.UnregisterValueChangedCallback(OnFriendlyCountSliderChanged);
        }

        private void OnCancelClicked()
        {
            GameUi.Pop();
        }

        private void LaunchCombat()
        {
            if (_snapshotFilePaths.Count == 0)
            {
                Debug.LogError($"[{nameof(FreeModeSetupController)}] No ship snapshots found!");
                return;
            }

            var selectedIndex = Mathf.Max(0, _snapshotDisplayNames.IndexOf(_shipDropdown.value));
            var selectedName = _snapshotDisplayNames[selectedIndex];
            var selectedFile = _snapshotFilePaths[selectedIndex];

            SaveState.Mode = GameSessionMode.FreeMode;
            SaveState.PlayerShipName = selectedName;
            SaveState.PlayerShipSnapshotFilePath = selectedFile;
            SaveState.AsteroidCount = Mathf.RoundToInt(_asteroidCountSlider.value);
            SaveState.EnemyShipCount = Mathf.RoundToInt(_enemyCountSlider.value);
            SaveState.FriendlyShipCount = Mathf.RoundToInt(_friendlyCountSlider.value);

            SceneManager.LoadScene(SceneNames.MainGame);
        }

        private void OnSnapshotCatalogChanged()
        {
            ApplySnapshotsToDropdown(_snapshotRepository.Model.Snapshots);
        }

        private void RefreshShipSnapshots()
        {
            ApplySnapshotsToDropdown(_snapshotRepository.Model.Snapshots);
        }

        private void ApplySnapshotsToDropdown(IReadOnlyList<SavedShipSnapshotDescriptor> snapshots)
        {
            _snapshotDisplayNames.Clear();
            _snapshotFilePaths.Clear();

            foreach (var snapshot in snapshots)
            {
                _snapshotDisplayNames.Add(snapshot.DisplayName);
                _snapshotFilePaths.Add(snapshot.FilePath);
            }

            if (_snapshotDisplayNames.Count == 0) GameUi.Notify("No ships exist!", PopupLevel.Warning);

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