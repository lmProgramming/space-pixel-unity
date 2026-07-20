using System;
using System.Collections.Generic;
using UI.MVCVM;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Scenes.BattleShipPicker.Views
{
    public class BattleShipPickerView : View<BattleShipPickerViewModel>
    {
        private readonly List<(VisualElement element, EventCallback<ClickEvent> callback)> _entryClickCallbacks = new();
        private readonly List<VisualElement> _entryElements = new();

        private Button _confirmButton;
        private VisualElement _list;
        private int? _selectedAllyIndex;
        private BattleShipPickerViewModel _viewModel;

        public event Action<int> ConfirmClicked;

        public override void BindUI(VisualElement root)
        {
            _list = root.Q<VisualElement>("battle-ship-picker-list");
            _confirmButton = root.Q<Button>("battle-ship-picker-confirm-button");

            if (_list == null || _confirmButton == null)
                throw new InvalidOperationException("[BattleShipPickerView] Required controls are missing in UXML.");

            _confirmButton.clicked += OnConfirmClicked;
            Render();
        }

        public override void UnbindUI()
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
            var row = new VisualElement();
            row.AddToClassList("ds-nav-item");

            if (entry.PreviewSprite)
            {
                var icon = new Image
                {
                    sprite = entry.PreviewSprite,
                    scaleMode = ScaleMode.ScaleToFit
                };
                icon.AddToClassList("ds-nav-item__icon");
                row.Add(icon);
            }

            var label = new Label(entry.DisplayName);
            label.AddToClassList("ds-nav-item__label");
            row.Add(label);

            EventCallback<ClickEvent> clickHandler = _ => SelectEntry(entry.AllyIndex);
            row.RegisterCallback(clickHandler);
            _entryClickCallbacks.Add((row, clickHandler));
            _entryElements.Add(row);
            _list.Add(row);
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
                _entryElements[index].EnableInClassList("is-active", allyIndex == _selectedAllyIndex);
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