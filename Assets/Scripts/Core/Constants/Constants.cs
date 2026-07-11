using System.IO;
using UnityEngine;

namespace Core.Constants
{
    public static class Constants
    {
        public const string PlayerShipId = "PlayerShip";

        public static readonly string DefaultSaveFolder = Path.Combine(Application.persistentDataPath, "ShipSnapshots");
    }
}