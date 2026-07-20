using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Gameplay;
using Core.Gameplay.Progression;
using Core.Services;
using Core.Ships;
using Core.ShipSnapshots;
using Core.State;
using Ships;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace UI.Scenes.MainMenu.Views.Progression
{
    public class NewCampaignController
        : Controller<ShipSnapshotCatalogModel, NewCampaignView, NewCampaignViewModel>
    {
        [SerializeField]
        private DesignShip previewShip;

        private string _campaignName = string.Empty;
        private int? _pendingSlotIndex;
        [Inject] private IProgressionRepository _progressionRepository;
        private int? _selectedShipIndex;

        [Inject] private IShipSnapshotRepository _snapshotRepository;
        [Inject] private IShipSnapshotService _snapshotService;

        protected override void OnEnable()
        {
            base.OnEnable();
            View.BackClicked += OnBackClicked;
            View.StartClicked += OnStartClicked;
            View.ShipSelected += OnShipSelected;
            View.CampaignNameChanged += OnCampaignNameChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            View.BackClicked -= OnBackClicked;
            View.StartClicked -= OnStartClicked;
            View.ShipSelected -= OnShipSelected;
            View.CampaignNameChanged -= OnCampaignNameChanged;
        }

        public event Action CloseSelected;

        public void OpenForSlot(int slotIndex)
        {
            _pendingSlotIndex = slotIndex;
            _selectedShipIndex = null;
            _campaignName = string.Empty;
            Refresh();
        }

        protected override ShipSnapshotCatalogModel CreateModel()
        {
            return _snapshotRepository.Model;
        }

        protected override NewCampaignView CreateView()
        {
            return gameObject.AddComponent<NewCampaignView>();
        }

        protected override NewCampaignViewModel CreateViewModel(ShipSnapshotCatalogModel model)
        {
            return new NewCampaignViewModel(
                CreateShipEntries(model.Snapshots),
                _campaignName,
                CanStartCampaign(),
                _selectedShipIndex);
        }

        private static IReadOnlyList<SavedShipSnapshotDescriptor> CreateShipEntries(
            IReadOnlyList<SavedShipSnapshotDescriptor> snapshots)
        {
            var entries = new SavedShipSnapshotDescriptor[snapshots.Count];

            for (var index = 0; index < snapshots.Count; index++)
            {
                var snapshot = snapshots[index];
                entries[index] = new SavedShipSnapshotDescriptor(
                    snapshot.DisplayName,
                    snapshot.FilePath,
                    snapshot.PreviewSprite);
            }

            return entries;
        }

        private void OnBackClicked()
        {
            CloseSelected?.Invoke();
            Hide();
        }

        private void OnStartClicked()
        {
            if (!_pendingSlotIndex.HasValue || !_selectedShipIndex.HasValue || !CanStartCampaign())
                return;

            var snapshotPath = Model.Snapshots[_selectedShipIndex.Value].FilePath;
            var snapshot = _snapshotService.LoadSnapshotFromFile(snapshotPath);

            var save = new ProgressionSave
            {
                campaignName = _campaignName.Trim(),
                allies = new[] { snapshot },
                credits = "0",
                enemiesKilled = 0
            };

            _progressionRepository.Save(_pendingSlotIndex.Value, save);

            SaveState.Mode = GameSessionMode.Progression;
            SaveState.ProgressionSlotIndex = _pendingSlotIndex.Value;

            SceneManager.LoadScene(SceneNames.BattleShipPicker);
        }

        private void OnShipSelected(int shipIndex)
        {
            _selectedShipIndex = shipIndex;

            if (!previewShip)
                throw new InvalidOperationException(
                    "[ProgressionNewCampaignController] Preview ship is not assigned.");

            var snapshotPath = Model.Snapshots[shipIndex].FilePath;
            _snapshotService.ApplySnapshot(previewShip, _snapshotService.LoadSnapshotFromFile(snapshotPath));
            previewShip.InitializeModules();
            View.ResourcesPanel.Refresh(previewShip);
            Refresh();
        }

        private void OnCampaignNameChanged(string campaignName)
        {
            _campaignName = campaignName ?? string.Empty;
            Refresh();
        }

        private bool CanStartCampaign()
        {
            return _selectedShipIndex.HasValue && !string.IsNullOrWhiteSpace(_campaignName);
        }
    }
}