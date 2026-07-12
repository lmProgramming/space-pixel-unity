using System;
using System.Collections.Generic;
using System.IO;
using Core.Constants;
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

        public IReadOnlyList<SavedShipSnapshotDescriptor> GetSavedSnapshots(
            string folderPath = null)
        {
            folderPath ??= Constants.DefaultSaveFolder;

            if (!Directory.Exists(folderPath))
                return Array.Empty<SavedShipSnapshotDescriptor>();

            var snapshotPaths = Directory.GetFiles(folderPath, "*.json");
            Array.Sort(snapshotPaths, StringComparer.OrdinalIgnoreCase);

            var descriptors = new SavedShipSnapshotDescriptor[snapshotPaths.Length];
            for (var index = 0; index < snapshotPaths.Length; index++)
            {
                var snapshotPath = snapshotPaths[index];
                descriptors[index] = new SavedShipSnapshotDescriptor(
                    Path.GetFileNameWithoutExtension(snapshotPath),
                    snapshotPath);
            }

            return descriptors;
        }

        public void DeleteSnapshotFile(string snapshotPath)
        {
            File.Delete(snapshotPath);
        }
    }
}