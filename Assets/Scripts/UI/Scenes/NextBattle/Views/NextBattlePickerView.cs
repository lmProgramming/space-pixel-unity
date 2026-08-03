using System;
using System.Collections.Generic;
using Core.Progression;
using UI.Components;
using UI.MVCVM;
using UnityEngine.UIElements;

namespace UI.Scenes.NextBattle.Views
{
    public class NextBattlePickerView : View<NextBattlePickerViewModel>
    {
        private readonly List<ShipPickerRow> _entryElements = new();

        private Button _confirmButton;
        private Button _hangarButton;
        private VisualElement _list;
        private Guid? _selectedBattleId;
        private NextBattlePickerViewModel _viewModel;

        public event Action<Guid> ConfirmClicked;
        public event Action HangarClicked;

        protected override void BindUiCore(VisualElement root)
        {
            _list = root.Q<VisualElement>("next-battle-picker-list");
            _confirmButton = root.Q<Button>("next-battle-picker-confirm-button");
            _hangarButton = root.Q<Button>("next-battle-picker-hangar-button");

            if (_list == null || _confirmButton == null || _hangarButton == null)
                throw new InvalidOperationException("[NextBattlePickerView] Required controls are missing in UXML.");

            _confirmButton.clicked += OnConfirmClicked;
            _hangarButton.clicked += OnHangarClicked;
            Render();
        }

        protected override void UnbindUiCore()
        {
            if (_confirmButton != null)
                _confirmButton.clicked -= OnConfirmClicked;

            if (_hangarButton != null)
                _hangarButton.clicked -= OnHangarClicked;

            ClearEntries();
            _list = null;
            _confirmButton = null;
            _hangarButton = null;
        }

        public override void SetData(NextBattlePickerViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _selectedBattleId = null;
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
            var row = ShipPickerRow.Create();
            row.Bind(entry.PreviewSprite, $"{entry.DisplayName}  (+{entry.CreditsReward} cr)",
                () => SelectEntry(entry.Id));
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

        private void SelectEntry(Guid battleId)
        {
            _selectedBattleId = battleId;
            UpdateSelectionVisuals();
        }

        private void UpdateSelectionVisuals()
        {
            for (var index = 0; index < _entryElements.Count; index++)
            {
                var battleId = _viewModel.Entries[index].Id;
                _entryElements[index].SetSelected(battleId == _selectedBattleId);
            }

            _confirmButton.SetEnabled(_selectedBattleId.HasValue);
        }

        private void OnConfirmClicked()
        {
            if (!_selectedBattleId.HasValue)
                return;

            ConfirmClicked?.Invoke(_selectedBattleId.Value);
        }

        private void OnHangarClicked()
        {
            HangarClicked?.Invoke();
        }
    }
}