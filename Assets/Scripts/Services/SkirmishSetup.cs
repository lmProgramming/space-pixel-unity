using Core.State;
using Ships;
using UnityEngine;

namespace Services
{
    public class SkirmishSetup : MonoBehaviour
    {
        private Ship _playerShip;

        private void Start()
        {
            var playerShipName = SaveState.PlayerShipName;
            var playerShipSnapshotFile = SaveState.PlayerShipSnapshotFile;
        }
    }
}