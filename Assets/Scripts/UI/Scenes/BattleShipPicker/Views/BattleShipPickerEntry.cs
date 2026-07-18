using UnityEngine;

namespace UI.Scenes.BattleShipPicker.Views
{
    public readonly struct BattleShipPickerEntry
    {
        public BattleShipPickerEntry(int allyIndex, string displayName, Sprite previewSprite)
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