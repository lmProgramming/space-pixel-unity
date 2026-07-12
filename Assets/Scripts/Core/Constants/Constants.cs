using System.IO;
using UnityEngine;

namespace Core.Constants
{
    public static class Constants
    {
        public const string PlayerShipId = "PlayerShip";

        public const string ShipSnapshotExtension = ".json";
        public const string ShipSnapshotIconExtension = ".png";

        public static readonly string ShipSnapshotsFolder =
            Path.Combine(Application.persistentDataPath, "ShipSnapshots");
    }
}