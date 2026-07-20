using System.Collections.Generic;
using Core.MVCVM;

namespace Core.Progression
{
    public class ProgressionSlotsModel : ObservableModel
    {
        private readonly List<ProgressionSlotDescriptor> _slots = new();

        public IReadOnlyList<ProgressionSlotDescriptor> Slots => _slots;

        public void ReplaceAll(IReadOnlyList<ProgressionSlotDescriptor> slots)
        {
            _slots.Clear();

            if (slots != null)
                _slots.AddRange(slots);

            NotifyChanged();
        }
    }
}