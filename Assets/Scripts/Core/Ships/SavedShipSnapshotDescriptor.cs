using UnityEngine;

namespace Core.Ships
{
    public class SavedShipSnapshotDescriptor
    {
        public SavedShipSnapshotDescriptor(
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