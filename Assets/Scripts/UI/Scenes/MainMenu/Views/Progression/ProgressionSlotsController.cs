using System;
using Core.Constants;
using Core.Services;
using Core.UI;
using UI.Common;
using UnityEngine.UIElements;
using Zenject;

namespace UI.Scenes.MainMenu.Views.Progression
{
    public class ProgressionSlotsController : PanelRendererBase
    {
        private const string DeleteConfirmOptionId = "confirm-delete";

        private readonly ProgressionSlotUiBinding[] _slotBindings =
            new ProgressionSlotUiBinding[Constants.ProgressionSlotCount];

        private Button _cancelButton;

        [Inject] private IProgressionRepository _progressionRepository;
        private int? _slotPendingDelete;

        public event Action<int> NewGameRequested;

        public event Action<int> LoadRequested;

        protected override void BindUiCore(VisualElement root)
        {
            _cancelButton = root.Q<Button>("progression-slots-cancel-button");
            if (_cancelButton == null)
                throw new InvalidOperationException(
                    "[ProgressionSlotsController] Cancel button is missing in template.");

            BindProgressionSlotRows(root);
            _cancelButton.clicked += OnCancelClicked;
            _progressionRepository.Model.Changed += OnProgressionSlotsChanged;
            RefreshProgressionSlots();
        }

        protected override void UnbindUiCore()
        {
            if (_cancelButton != null)
                _cancelButton.clicked -= OnCancelClicked;

            _progressionRepository.Model.Changed -= OnProgressionSlotsChanged;
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
                        $"[ProgressionSlotsController] Progression slot UI for index {slotIndex} is missing.");

                var capturedSlotIndex = slotIndex;
                slotButton.clicked += () => OnProgressionSlotClicked(capturedSlotIndex);
                deleteButton.clicked += () => OnProgressionSlotDeleteClicked(capturedSlotIndex);
                row.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (row.ClassListContains("has-save"))
                        deleteButton.visible = true;
                });
                row.RegisterCallback<MouseLeaveEvent>(_ => deleteButton.visible = false);

                _slotBindings[slotIndex] = new ProgressionSlotUiBinding(row, slotButton, deleteButton);
            }
        }

        private void UnbindProgressionSlotRows()
        {
            for (var slotIndex = 0; slotIndex < _slotBindings.Length; slotIndex++)
                _slotBindings[slotIndex] = null;
        }

        private void OnCancelClicked()
        {
            GameUi.Pop();
        }

        private void OnProgressionSlotsChanged()
        {
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

                binding.DeleteButton.visible = false;
                binding.Row.EnableInClassList("has-save", descriptor.HasSave);
            }
        }

        private void OnProgressionSlotClicked(int slotIndex)
        {
            var descriptor = _progressionRepository.Model.Slots[slotIndex];

            if (!descriptor.HasSave)
            {
                NewGameRequested?.Invoke(slotIndex);
                return;
            }

            LoadRequested?.Invoke(slotIndex);
        }

        private void OnProgressionSlotDeleteClicked(int slotIndex)
        {
            _slotPendingDelete = slotIndex;
            GameUi.ShowOptions(
                "Delete save?",
                "This progression save will be permanently deleted.",
                OnDeleteOptionSelected,
                new OptionsPopupOption("cancel", "Cancel", OptionsPopupOptionStyle.Ghost),
                new OptionsPopupOption(DeleteConfirmOptionId, "Delete", OptionsPopupOptionStyle.Danger));
        }

        private void OnDeleteOptionSelected(string optionId)
        {
            if (optionId != DeleteConfirmOptionId || !_slotPendingDelete.HasValue)
            {
                _slotPendingDelete = null;
                return;
            }

            _progressionRepository.Delete(_slotPendingDelete.Value);
            _slotPendingDelete = null;
            RefreshProgressionSlots();
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