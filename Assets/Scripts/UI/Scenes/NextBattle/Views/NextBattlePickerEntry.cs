using UnityEngine;

namespace UI.Scenes.NextBattle.Views
{
    public readonly struct NextBattlePickerEntry
    {
        public NextBattlePickerEntry(int allyIndex, string displayName, Sprite previewSprite)
        {
            AllyIndex = allyIndex;
            DisplayName = displayName;
            PreviewSprite = previewSprite;
        }

        public int AllyIndex { get; }

        public string DisplayName { get; }

        public Sprite PreviewSprite { get; }
    }
}