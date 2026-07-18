using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Services;
using Core.Ships;
using Core.State;
using UI.Common;
using UI.Scenes.MainMenu.Views.Progression;
using UI.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Scenes.MainMenu
{
    public class MainMenuController : PanelRendererBase
    {
        private const float DefaultAsteroidCount = 6f;
        private const float DefaultEnemyShipCount = 0f;
        private const float DefaultFriendlyShipCount = 0f;
        [SerializeField] private SettingsOverlayController settingsOverlay;

        [SerializeField]
        private NewCampaignController newCampaignController;

        private readonly ProgressionSlotUiBinding[] _slotBindings =
            new ProgressionSlotUiBinding[Constants.ProgressionSlotCount];

        private readonly List<string> _snapshotDisplayNames = new();
        private readonly List<string> _snapshotFilePaths = new();

        private Slider _asteroidCountSlider;
        private Slider _enemyCountSlider;
        private Slider _friendlyCountSlider;
        private Button _progressionButton;
        private Button _progressionDeleteCancelButton;
        private Button _progressionDeleteConfirmButton;
        private VisualElement _progressionDeleteConfirmOverlay;
        [Inject] private IProgressionRepository _progressionRepository;
        private Button _progressionSlotsCancelButton;
        private VisualElement _progressionSlotsOverlay;
        private Button _quitButton;
        private Button _settingsButton;
        private DropdownField _shipDropdown;
        private Button _shipFactoryButton;
        private Button _shipSelectCancelButton;
        private VisualElement _shipSelectionOverlay;
        private Button _shipSelectLaunchButton;
        private int? _slotPendingDelete;

        [Inject] private IShipSnapshotRepository _snapshotRepository;

        private Button _startButton;

        private void Start()
        {
            if (_snapshotRepository == null)
                throw new InvalidOperationException("[MainMenuController] Snapshot repository is not initialized.");

            if (_progressionRepository == null)
                throw new InvalidOperationException("[MainMenuController] Progression repository is not initialized.");
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            _progressionRepository.Model.Changed -= OnProgressionSlotsChanged;
        }

        protected override void BindUiCore(VisualElement root)
        {
            _startButton = root.Q<Button>("start-button");
            _progressionButton = root.Q<Button>("progression-button");
            _shipFactoryButton = root.Q<Button>("ship-factory-button");
            _settingsButton = root.Q<Button>("settings-button");
            _quitButton = root.Q<Button>("quit-button");
            _progressionSlotsOverlay = root.Q<VisualElement>("progression-slots-overlay");
            _progressionSlotsCancelButton = root.Q<Button>("progression-slots-cancel-button");
            _progressionDeleteConfirmOverlay = root.Q<VisualElement>("progression-delete-confirm-overlay");
            _progressionDeleteCancelButton = root.Q<Button>("progression-delete-cancel-button");
            _progressionDeleteConfirmButton = root.Q<Button>("progression-delete-confirm-button");
            _shipSelectionOverlay = root.Q<VisualElement>("ship-select-overlay");
            _shipDropdown = root.Q<DropdownField>("ship-dropdown");
            _shipSelectCancelButton = root.Q<Button>("ship-select-cancel-button");
            _shipSelectLaunchButton = root.Q<Button>("ship-select-launch-button");
            _asteroidCountSlider = root.Q<Slider>("asteroid-count-slider");
            _enemyCountSlider = root.Q<Slider>("enemy-count-slider");
            _friendlyCountSlider = root.Q<Slider>("friendly-count-slider");

            if (_startButton == null || _progressionButton == null || _shipFactoryButton == null ||
                _settingsButton == null || _quitButton == null || _progressionSlotsOverlay == null ||
                _progressionSlotsCancelButton == null || _progressionDeleteConfirmOverlay == null ||
                _progressionDeleteCancelButton == null || _progressionDeleteConfirmButton == null ||
                _shipSelectionOverlay == null || _shipDropdown == null || _shipSelectCancelButton == null ||
                _shipSelectLaunchButton == null || _asteroidCountSlider == null || _enemyCountSlider == null ||
                _friendlyCountSlider == null)
                throw new InvalidOperationException(
                    "[MainMenuController] Required UI elements are missing in template.");

            BindProgressionSlotRows(root);

            _progressionButton.clicked += OpenProgressionSlotsOverlay;
            _startButton.clicked += OpenShipSelectionDialog;
            _shipFactoryButton.clicked += OpenShipFactory;
            _settingsButton.clicked += OpenSettings;
            _quitButton.clicked += QuitGame;
            _progressionSlotsCancelButton.clicked += CloseProgressionSlotsOverlay;
            _progressionDeleteCancelButton.clicked += CloseDeleteConfirmOverlay;
            _progressionDeleteConfirmButton.clicked += ConfirmDeleteSlot;
            _shipSelectCancelButton.clicked += CloseShipSelectionDialog;
            _shipSelectLaunchButton.clicked += LaunchCombat;
            ConfigureCountSlider(_asteroidCountSlider, DefaultAsteroidCount, OnAsteroidCountSliderChanged);
            ConfigureCountSlider(_enemyCountSlider, DefaultEnemyShipCount, OnEnemyCountSliderChanged);
            ConfigureCountSlider(_friendlyCountSlider, DefaultFriendlyShipCount, OnFriendlyCountSliderChanged);

            newCampaignController.CloseSelected += Show;

            DesignSystemThemeService.RegisterVisualTree(root);

            if (!settingsOverlay)
                throw new InvalidOperationException(
                    "[MainMenuController] SettingsOverlayController is not assigned.");

            if (!newCampaignController)
                throw new InvalidOperationException(
                    "[MainMenuController] ProgressionNewCampaignController is not assigned.");
        }

        protected override void UnbindUiCore()
        {
            _progressionButton.clicked -= OpenProgressionSlotsOverlay;
            _startButton.clicked -= OpenShipSelectionDialog;
            _shipFactoryButton.clicked -= OpenShipFactory;
            _settingsButton.clicked -= OpenSettings;
            _quitButton.clicked -= QuitGame;
            _progressionSlotsCancelButton.clicked -= CloseProgressionSlotsOverlay;
            _progressionDeleteCancelButton.clicked -= CloseDeleteConfirmOverlay;
            _progressionDeleteConfirmButton.clicked -= ConfirmDeleteSlot;
            _shipSelectCancelButton.clicked -= CloseShipSelectionDialog;
            _shipSelectLaunchButton.clicked -= LaunchCombat;
            _progressionRepository.Model.Changed -= OnProgressionSlotsChanged;
            _asteroidCountSlider.UnregisterValueChangedCallback(OnAsteroidCountSliderChanged);
            _enemyCountSlider.UnregisterValueChangedCallback(OnEnemyCountSliderChanged);
            _friendlyCountSlider.UnregisterValueChangedCallback(OnFriendlyCountSliderChanged);

            newCampaignController.CloseSelected -= Show;

            UnbindProgressionSlotRows();
        }

        private void BindProgressionSlotRows(VisualElement root)
        {
            for (var slotIndex = 0; slotIndex < Constants.ProgressionSlotCount; slotIndex++)
            {
                var row = root.Q<VisualElement>($"progression-slot-{slotIndex}-row");
                var slotButton = root.Q<Button>($"progression-slot-{slotIndex}-button");
                var deleteButton = root.Q<Button>($"progression-slot-{slotIndex}-delete-button");

                if (row == null || slotButton == null || deleteButton == null)
                    throw new InvalidOperationException(
                        $"[MainMenuController] Progression slot UI for index {slotIndex} is missing.");

                var capturedSlotIndex = slotIndex;
                slotButton.clicked += () => OnProgressionSlotClicked(capturedSlotIndex);
                deleteButton.clicked += () => OnProgressionSlotDeleteClicked(capturedSlotIndex);
                row.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (row.ClassListContains("has-save"))
                        deleteButton.style.display = DisplayStyle.Flex;
                });
                row.RegisterCallback<MouseLeaveEvent>(_ => deleteButton.style.display = DisplayStyle.None);

                _slotBindings[slotIndex] = new ProgressionSlotUiBinding(row, slotButton, deleteButton);
            }
        }

        private void UnbindProgressionSlotRows()
        {
            for (var slotIndex = 0; slotIndex < _slotBindings.Length; slotIndex++)
                _slotBindings[slotIndex] = null;
        }

        private void OpenProgressionSlotsOverlay()
        {
            _progressionRepository.Model.Changed += OnProgressionSlotsChanged;
            RefreshProgressionSlots();
            _progressionSlotsOverlay.style.display = DisplayStyle.Flex;
        }

        private void CloseProgressionSlotsOverlay()
        {
            _progressionRepository.Model.Changed -= OnProgressionSlotsChanged;
            _progressionSlotsOverlay.style.display = DisplayStyle.None;
            CloseDeleteConfirmOverlay();
        }

        private void OnProgressionSlotsChanged()
        {
            if (_progressionSlotsOverlay.style.display == DisplayStyle.None)
                return;

            RefreshProgressionSlots();
        }

        private void RefreshProgressionSlots()
        {
            var slots = _progressionRepository.Model.Slots;

            for (var slotIndex = 0; slotIndex < Constants.ProgressionSlotCount; slotIndex++)
            {
                var descriptor = slots[slotIndex];
                var binding = _slotBindings[slotIndex];

                binding.SlotButton.text = descriptor.HasSave
                    ? $"Load {descriptor.CampaignName}"
                    : "New game";

                binding.DeleteButton.style.display = DisplayStyle.None;
                binding.Row.EnableInClassList("has-save", descriptor.HasSave);
            }
        }

        private void OnProgressionSlotClicked(int slotIndex)
        {
            var descriptor = _progressionRepository.Model.Slots[slotIndex];

            if (!descriptor.HasSave)
            {
                OpenNewProgressionOverlay(slotIndex);
                return;
            }

            SaveState.Mode = GameSessionMode.Progression;
            SaveState.ProgressionSlotIndex = slotIndex;
            SceneManager.LoadScene(SceneNames.BattleShipPicker);
        }

        private void OpenNewProgressionOverlay(int slotIndex)
        {
            CloseProgressionSlotsOverlay();
            newCampaignController.OpenForSlot(slotIndex);
            Hide();
        }

        private void Show()
        {
            gameObject.SetActive(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnProgressionSlotDeleteClicked(int slotIndex)
        {
            _slotPendingDelete = slotIndex;
            _progressionDeleteConfirmOverlay.style.display = DisplayStyle.Flex;
        }

        private void CloseDeleteConfirmOverlay()
        {
            _slotPendingDelete = null;
            _progressionDeleteConfirmOverlay.style.display = DisplayStyle.None;
        }

        private void ConfirmDeleteSlot()
        {
            if (!_slotPendingDelete.HasValue)
                return;

            _progressionRepository.Delete(_slotPendingDelete.Value);
            CloseDeleteConfirmOverlay();
            RefreshProgressionSlots();
        }

        private void OpenShipSelectionDialog()
        {
            _snapshotRepository.Model.Changed += OnSnapshotCatalogChanged;
            RefreshShipSnapshots();
            _shipSelectionOverlay.style.display = DisplayStyle.Flex;
        }

        private void CloseShipSelectionDialog()
        {
            _snapshotRepository.Model.Changed -= OnSnapshotCatalogChanged;
            _shipSelectionOverlay.style.display = DisplayStyle.None;
        }

        private static void OpenShipFactory()
        {
            SceneManager.LoadScene(SceneNames.ShipFactory);
        }

        private void OpenSettings()
        {
            settingsOverlay.Toggle();
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
            if (_shipSelectionOverlay == null ||
                _shipSelectionOverlay.style.display == DisplayStyle.None)
                return;

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

        private sealed class ProgressionSlotUiBinding
        {
            public ProgressionSlotUiBinding(VisualElement row, Button slotButton, Button deleteButton)
            {
                Row = row;
                SlotButton = slotButton;
                DeleteButton = deleteButton;
            }

            public VisualElement Row { get; }

            public Button SlotButton { get; }

            public Button DeleteButton { get; }
        }
    }
}