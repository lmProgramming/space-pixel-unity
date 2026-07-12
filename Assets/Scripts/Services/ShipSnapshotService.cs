using System.IO;
using Core.Services;
using Core.Ships;
using LMPro.External.IsAlive;
using UnityEngine;
using Zenject;

namespace Services
{
    public class ShipSnapshotService : IShipSnapshotService
    {
        private readonly IGameContentCatalog _gameContentCatalog;

        [Inject]
        public ShipSnapshotService(IGameContentCatalog gameContentCatalog)
        {
            _gameContentCatalog = gameContentCatalog;
        }

        public ShipSnapshot CaptureSnapshot(IShip ship)
        {
            if (!ship.IsAlive())
            {
                Debug.LogError("[ShipSnapshotService] Cannot capture snapshot: ship is null");
                return null;
            }

            return ship.CaptureSnapshot(_gameContentCatalog);
        }

        public void ApplySnapshot(IShip ship, ShipSnapshot snapshot)
        {
            if (!ship.IsAlive())
            {
                Debug.LogError("[ShipSnapshotService] Cannot apply snapshot: ship is null");
                return;
            }

            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotService] Cannot apply snapshot: snapshot is null");
                return;
            }

            ship.RestoreFromSnapshot(snapshot, _gameContentCatalog);
        }

        public ShipSnapshot LoadSnapshotFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var snapshot = JsonUtility.FromJson<ShipSnapshot>(json);

            return snapshot;
        }
    }
}