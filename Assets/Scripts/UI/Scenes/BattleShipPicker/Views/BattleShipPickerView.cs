using System;
using System.Collections.Generic;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Scenes.BattleShipPicker.Views
{
    public class BattleShipPickerView : View<BattleShipPickerViewModel>
    {
        private const string ShipRowTemplatePath = "UI/ShipRowTemplate";

        private readonly List<(VisualElement element, EventCallback<ClickEvent> callback)> _entryClickCallbacks = new();
        private readonly List<VisualElement> _entryElements = new();

        private Button _confirmButton;
        private VisualElement _list;
        private int? _selectedAllyIndex;
        private VisualTreeAsset _shipRowTemplate;
        private BattleShipPickerViewModel _viewModel;

        public event Action<int> ConfirmClicked;

        protected override void BindUiCore(VisualElement root)
        {
            _list = root.Q<VisualElement>("battle-ship-picker-list");
            _confirmButton = root.Q<Button>("battle-ship-picker-confirm-button");

            if (_list == null || _confirmButton == null)
                throw new InvalidOperationException("[BattleShipPickerView] Required controls are missing in UXML.");

            _shipRowTemplate = Resources.Load<VisualTreeAsset>(ShipRowTemplatePath);
            if (!_shipRowTemplate)
                throw new InvalidOperationException(
                    $"[BattleShipPickerView] VisualTreeAsset '{ShipRowTemplatePath}' was not found in Resources.");

            _confirmButton.clicked += OnConfirmClicked;
            Render();
        }

        protected override void UnbindUiCore()
        {
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;

            ClearEntries();
            _list = null;
            _confirmButton = null;
        }

        public override void SetData(BattleShipPickerViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _selectedAllyIndex = null;
            Render();
        }

        private void Render()
        {
            if (_viewModel == null || _list == null || _confirmButton == null)
                return;

            ClearEntries();

            foreach (var battleShipPickerEntry in _viewModel.Entries)
                AddEntry(battleShipPickerEntry);

            UpdateSelectionVisuals();
        }

        private void AddEntry(BattleShipPickerEntry entry)
        {
            var rowIndex = _list.childCount;
            _shipRowTemplate.CloneTree(_list);
            var row = _list[rowIndex].Q<VisualElement>("ship-row")
                      ?? throw new InvalidOperationException(
                          "[BattleShipPickerView] 'ship-row' is missing in ShipRowTemplate.uxml.");

            var icon = row.Q<Image>("ship-row-icon")
                       ?? throw new InvalidOperationException(
                           "[BattleShipPickerView] 'ship-row-icon' is missing in ShipRowTemplate.uxml.");
            var label = row.Q<Label>("ship-row-label")
                        ?? throw new InvalidOperationException(
                            "[BattleShipPickerView] 'ship-row-label' is missing in ShipRowTemplate.uxml.");

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

            EventCallback<ClickEvent> clickHandler = _ => SelectEntry(entry.AllyIndex);
            row.RegisterCallback(clickHandler);
            _entryClickCallbacks.Add((row, clickHandler));
            _entryElements.Add(row);
        }

        private void ClearEntries()
        {
            foreach (var (element, callback) in _entryClickCallbacks)
                element.UnregisterCallback(callback);

            _entryClickCallbacks.Clear();
            _entryElements.Clear();
            _list?.Clear();
        }

        private void SelectEntry(int allyIndex)
        {
            _selectedAllyIndex = allyIndex;
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (var index = 0; index < _entryElements.Count; index++)
            {
                var allyIndex = _viewModel.Entries[index].AllyIndex;
                _entryElements[index].EnableInClassList("is-selected", allyIndex == _selectedAllyIndex);
            }

            _confirmButton.SetEnabled(_selectedAllyIndex.HasValue);
        }

        private void OnConfirmClicked()
        {
            if (!_selectedAllyIndex.HasValue)
                return;

            ConfirmClicked?.Invoke(_selectedAllyIndex.Value);
        }
    }
}