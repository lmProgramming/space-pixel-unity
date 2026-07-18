using System.Collections.Generic;

namespace UI.Scenes.BattleShipPicker.Views
{
    public class BattleShipPickerViewModel
    {
        public BattleShipPickerViewModel(IReadOnlyList<BattleShipPickerEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<BattleShipPickerEntry> Entries { get; }
    }
}