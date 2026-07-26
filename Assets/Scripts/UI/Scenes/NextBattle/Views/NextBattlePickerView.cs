using System;
using System.Collections.Generic;
using Core.Progression;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Scenes.NextBattle.Views
{
    public class NextBattlePickerView : View<NextBattlePickerViewModel>
    {
        private const string ShipRowTemplatePath = "UI/ShipRowTemplate";

        private readonly List<(VisualElement element, EventCallback<ClickEvent> callback)> _entryClickCallbacks = new();
        private readonly List<VisualElement> _entryElements = new();

        private Button _confirmButton;
        private VisualElement _list;
        private Guid? _selectedAllyIndex;
        private VisualTreeAsset _shipRowTemplate;
        private NextBattlePickerViewModel _viewModel;

        public event Action<Guid> ConfirmClicked;

        protected override void BindUiCore(VisualElement root)
        {
            _list = root.Q<VisualElement>("next-battle-picker-list");
            _confirmButton = root.Q<Button>("next-battle-picker-confirm-button");

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

        public override void SetData(NextBattlePickerViewModel viewModel)
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

            foreach (var nextBattlePickerEntry in _viewModel.Entries)
                AddEntry(nextBattlePickerEntry);

            UpdateSelectionVisuals();
        }

        private void AddEntry(NextBattlePickerEntry entry)
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
                    thumb.visible = false;
            }

            label.text = entry.DisplayName;

            EventCallback<ClickEvent> clickHandler = _ => SelectEntry(entry.Id);
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

        private void SelectEntry(Guid allyIndex)
        {
            _selectedAllyIndex = allyIndex;
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (var index = 0; index < _entryElements.Count; index++)
            {
                var allyIndex = _viewModel.Entries[index].Id;
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