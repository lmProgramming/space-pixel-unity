using Core;
using Core.Services;
using Core.Ship;
using Core.State;
using UnityEngine;
using Zenject;

namespace Services
{
    public class SkirmishSetup : MonoBehaviour
    {
        [Inject(Id = Constants.PlayerShipId)]
        private IShip _playerShip;

        [Inject]
        private IShipSnapshotService _snapshotService;

        private void Start()
        {
            var playerShipSnapshotFile = SaveState.PlayerShipSnapshotFilePath;

            _snapshotService.ApplySnapshot(_playerShip, _snapshotService.LoadSnapshotFromFile(playerShipSnapshotFile));
        }
    }
}