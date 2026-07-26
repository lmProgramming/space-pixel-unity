using System;
using System.Collections.Generic;
using Core.Ships;
using UI.Components;
using UI.MVCVM;
using UnityEngine.UIElements;

namespace UI.Scenes.MainMenu.Views.Progression
{
    public class NewCampaignView : View<NewCampaignViewModel>
    {
        private readonly List<ShipPickerRow> _shipRows = new();

        private Button _backButton;
        private TextField _campaignNameField;
        private VisualElement _resourcesContainer;
        private int? _selectedShipIndex;
        private VisualElement _shipList;
        private Button _startButton;
        private NewCampaignViewModel _viewModel;

        public ResourcesPanel ResourcesPanel { get; private set; }

        public VisualElement PreviewContainer { get; private set; }

        public event Action BackClicked;

        public event Action StartClicked;

        public event Action<int> ShipSelected;

        public event Action<string> CampaignNameChanged;

        protected override void BindUiCore(VisualElement root)
        {
            _shipList = root.Q<VisualElement>("progression-new-campaign-ship-list");
            _campaignNameField = root.Q<TextField>("progression-new-campaign-name-field");
            PreviewContainer = root.Q<VisualElement>("progression-new-campaign-preview-container");
            _resourcesContainer = root.Q<VisualElement>("progression-new-campaign-resources-container");
            _backButton = root.Q<Button>("progression-new-campaign-back-button");
            _startButton = root.Q<Button>("progression-new-campaign-start-button");

            ResourcesPanel = _resourcesContainer?.Q<ResourcesPanel>();

            if (_shipList == null || _campaignNameField == null || PreviewContainer == null ||
                _resourcesContainer == null || ResourcesPanel == null || _backButton == null ||
                _startButton == null)
                throw new InvalidOperationException(
                    "[ProgressionNewCampaignView] Required controls are missing in UXML.");

            ConfigureCampaignNameField();

            _backButton.clicked += OnBackClicked;
            _startButton.clicked += OnStartClicked;
            _campaignNameField.RegisterValueChangedCallback(OnCampaignNameChanged);

            Render();
        }

        private void ConfigureCampaignNameField()
        {
            _campaignNameField.textEdition.placeholder = "e.g. Operation Starfall";
            _campaignNameField.textEdition.hidePlaceholderOnFocus = true;
        }

        protected override void UnbindUiCore()
        {
            if (_backButton != null)
                _backButton.clicked -= OnBackClicked;
            if (_startButton != null)
                _startButton.clicked -= OnStartClicked;
            _campaignNameField?.UnregisterValueChangedCallback(OnCampaignNameChanged);
            ClearShipRows();
        }

        public override void SetData(NewCampaignViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _selectedShipIndex = viewModel.SelectedShipIndex;
            Render();
        }

        private void Render()
        {
            if (_viewModel == null || _shipList == null || _startButton == null || _campaignNameField == null)
                return;

            ClearShipRows();

            for (var index = 0; index < _viewModel.Ships.Count; index++)
                AddShipRow(_viewModel.Ships[index], index);

            _campaignNameField.SetValueWithoutNotify(_viewModel.CampaignName);
            _startButton.SetEnabled(_viewModel.CanStart);
            UpdateSelectionVisuals();
        }

        private void AddShipRow(SavedShipSnapshotDescriptor entry, int index)
        {
            var row = ShipPickerRow.Create();
            row.Bind(entry.PreviewSprite, entry.DisplayName, () => SelectShip(index));
            _shipList.Add(row);
            _shipRows.Add(row);
        }

        private void ClearShipRows()
        {
            foreach (var row in _shipRows)
                row.Unbind();

            _shipRows.Clear();
            _shipList?.Clear();
        }

        private void SelectShip(int index)
        {
            _selectedShipIndex = index;
            UpdateSelectionVisuals();
            ShipSelected?.Invoke(index);
        }

        private void UpdateSelectionVisuals()
        {
            for (var index = 0; index < _shipRows.Count; index++)
                _shipRows[index].SetSelected(index == _selectedShipIndex);
        }

        private void OnBackClicked()
        {
            BackClicked?.Invoke();
        }

        private void OnStartClicked()
        {
            StartClicked?.Invoke();
        }

        private void OnCampaignNameChanged(ChangeEvent<string> evt)
        {
            CampaignNameChanged?.Invoke(evt.newValue);
        }
    }
}