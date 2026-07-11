namespace Core.Ships
{
    public class SavedShipSnapshotDescriptor
    {
        public SavedShipSnapshotDescriptor(
            string displayName,
            string filePath)
        {
            DisplayName = displayName;
            FilePath = filePath;
        }

        public string DisplayName { get; }

        public string FilePath { get; }
    }
}