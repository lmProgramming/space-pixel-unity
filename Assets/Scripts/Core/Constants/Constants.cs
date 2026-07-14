using System.IO;
using UnityEngine;

namespace Core.Constants
{
    public static class Constants
    {
        public const string PlayerShipId = "PlayerShip";

        public const string ShipSnapshotExtension = ".json";
        public const string ShipSnapshotIconExtension = ".png";

        public const int ProgressionSlotCount = 3;

        public const string ProgressionSaveExtension = ".json";

        public static readonly string ShipSnapshotsFolder =
            Path.Combine(Application.persistentDataPath, "ShipSnapshots");

        public static readonly string ProgressionSavesFolder =
            Path.Combine(Application.persistentDataPath, "Progression");

        public static string ProgressionSlotFileName(int slotIndex)
        {
            return $"slot_{slotIndex}{ProgressionSaveExtension}";
        }
    }
}