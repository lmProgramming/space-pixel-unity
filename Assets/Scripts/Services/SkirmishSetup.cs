using Core;
using Core.Services;
using Core.Ship;
using Core.State;
using Services.Camera;
using UnityEngine;
using Zenject;

namespace Services
{
    public class SkirmishSetup : MonoBehaviour
    {
        [SerializeField] private CameraManager cameraManager;

        [Inject(Id = Constants.PlayerShipId)]
        private IShip _playerShip;

        [Inject]
        private IShipSnapshotService _snapshotService;

        private void Start()
        {
            var playerShipSnapshotFile = SaveState.PlayerShipSnapshotFilePath;

            if (playerShipSnapshotFile != null)
                _snapshotService.ApplySnapshot(_playerShip,
                    _snapshotService.LoadSnapshotFromFile(playerShipSnapshotFile));

            _playerShip.InitializeModules();

            cameraManager.StartFollowingObject((_playerShip.CommandModule as MonoBehaviour)?.gameObject);
        }
    }
}