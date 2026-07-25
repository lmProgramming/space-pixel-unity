using System.Collections.Generic;
using UI.Scenes.BattleShipPicker.Views;

namespace UI.Scenes.NextBattle.Views
{
    public class NextBattlePickerViewModel
    {
        public NextBattlePickerViewModel(IReadOnlyList<BattleShipPickerEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<BattleShipPickerEntry> Entries { get; }
    }
}