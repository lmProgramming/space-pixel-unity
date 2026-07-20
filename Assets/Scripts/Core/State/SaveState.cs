using Core.Gameplay;

namespace Core.State
{
    public static class SaveState
    {
        public static GameSessionMode Mode { get; set; }
        public static int ProgressionSlotIndex { get; set; }
        public static int SelectedAllyIndex { get; set; }
        public static string PlayerShipName { get; set; }
        public static string PlayerShipSnapshotFilePath { get; set; }
        public static int AsteroidCount { get; set; }
        public static int EnemyShipCount { get; set; }
        public static int FriendlyShipCount { get; set; }
    }
}