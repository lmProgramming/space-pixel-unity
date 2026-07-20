using System;
using System.Collections.Generic;
using Core.Ships;
using UI.Components;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.UIElements;
using Resources = UnityEngine.Resources;

namespace UI.Scenes.MainMenu.Views.Progression
{
    public class NewCampaignView : View<NewCampaignViewModel>
    {
        private const string ShipRowTemplatePath = "UI/ShipRowTemplate";

        private readonly List<(VisualElement element, EventCallback<ClickEvent> callback)> _shipClickCallbacks = new();
        private readonly List<VisualElement> _shipRows = new();

        private Button _backButton;
        private TextField _campaignNameField;
        private VisualElement _resourcesContainer;
        private int? _selectedShipIndex;
        private VisualElement _shipList;
        private VisualTreeAsset _shipRowTemplate;
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

            if (_shipList == null || _campaignNameField == null || PreviewContainer == null ||
                _resourcesContainer == null || _backButton == null || _startButton == null)
                throw new InvalidOperationException(
                    "[ProgressionNewCampaignView] Required controls are missing in UXML.");

            _shipRowTemplate = Resources.Load<VisualTreeAsset>(ShipRowTemplatePath);
            if (!_shipRowTemplate)
                throw new InvalidOperationException(
                    $"[ProgressionNewCampaignView] VisualTreeAsset '{ShipRowTemplatePath}' was not found in Resources.");

            ConfigureCampaignNameField();

            ResourcesPanel = new ResourcesPanel();
            _resourcesContainer.Add(ResourcesPanel);

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
            var rowIndex = _shipList.childCount;
            _shipRowTemplate.CloneTree(_shipList);
            var row = _shipList[rowIndex].Q<VisualElement>("ship-row")
                      ?? throw new InvalidOperationException(
                          "[ProgressionNewCampaignView] 'ship-row' is missing in ShipRowTemplate.uxml.");

            var icon = row.Q<Image>("ship-row-icon")
                       ?? throw new InvalidOperationException(
                           "[ProgressionNewCampaignView] 'ship-row-icon' is missing in ShipRowTemplate.uxml.");
            var label = row.Q<Label>("ship-row-label")
                        ?? throw new InvalidOperationException(
                            "[ProgressionNewCampaignView] 'ship-row-label' is missing in ShipRowTemplate.uxml.");

            if (entry.PreviewSprite)
            {
                icon.sprite = entry.PreviewSprite;
                icon.scaleMode = ScaleMode.ScaleToFit;
            }
            else
            {
                var thumb = row.Q("ship-row-thumb");
                if (thumb != null)
                    thumb.style.display = DisplayStyle.None;
            }

            label.text = entry.DisplayName;

            EventCallback<ClickEvent> clickHandler = _ => SelectShip(index);
            row.RegisterCallback(clickHandler);
            _shipClickCallbacks.Add((row, clickHandler));
            _shipRows.Add(row);
        }

        private void ClearShipRows()
        {
            foreach (var (element, callback) in _shipClickCallbacks)
                element.UnregisterCallback(callback);

            _shipClickCallbacks.Clear();
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
                _shipRows[index].EnableInClassList("is-selected", index == _selectedShipIndex);
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