using System.Collections.Generic;
using Core.Progression;

namespace UI.Scenes.NextBattle.Views
{
    public class NextBattlePickerViewModel
    {
        public NextBattlePickerViewModel(IReadOnlyList<NextBattlePickerEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<NextBattlePickerEntry> Entries { get; }
    }
}