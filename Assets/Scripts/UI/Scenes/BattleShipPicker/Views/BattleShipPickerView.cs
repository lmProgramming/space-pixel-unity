using System;
using System.Collections.Generic;
using UI.Components;
using UI.MVCVM;
using UnityEngine.UIElements;

namespace UI.Scenes.BattleShipPicker.Views
{
    public class BattleShipPickerView : View<BattleShipPickerViewModel>
    {
        private readonly List<ShipPickerRow> _entryElements = new();

        private Button _confirmButton;
        private VisualElement _list;
        private int? _selectedAllyIndex;
        private BattleShipPickerViewModel _viewModel;

        public event Action<int> ConfirmClicked;

        protected override void BindUiCore(VisualElement root)
        {
            _list = root.Q<VisualElement>("battle-ship-picker-list");
            _confirmButton = root.Q<Button>("battle-ship-picker-confirm-button");

            if (_list == null || _confirmButton == null)
                throw new InvalidOperationException("[BattleShipPickerView] Required controls are missing in UXML.");

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
            var row = ShipPickerRow.Create();
            row.Bind(entry.PreviewSprite, entry.DisplayName, () => SelectEntry(entry.AllyIndex));
            _list.Add(row);
            _entryElements.Add(row);
        }

        private void ClearEntries()
        {
            foreach (var row in _entryElements)
                row.Unbind();

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
                _entryElements[index].SetSelected(allyIndex == _selectedAllyIndex);
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