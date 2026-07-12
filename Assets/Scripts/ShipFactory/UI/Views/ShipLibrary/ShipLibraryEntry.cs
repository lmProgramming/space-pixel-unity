using UnityEngine;

namespace ShipFactory.UI.Views.ShipLibrary
{
    public sealed class ShipLibraryEntry
    {
        public ShipLibraryEntry(
            string displayName,
            string filePath,
            Sprite previewSprite)
        {
            DisplayName = displayName;
            FilePath = filePath;
            PreviewSprite = previewSprite;
        }

        public string DisplayName { get; }

        public string FilePath { get; }

        public Sprite PreviewSprite { get; }
    }
}