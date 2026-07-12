using System;
using System.Collections.Generic;
using System.IO;
using Core.Constants;
using Core.Services;
using Core.Ships;
using Core.ShipSnapshots;
using UnityEngine;

namespace Services
{
    public class ShipSnapshotRepository : IShipSnapshotRepository
    {
        public ShipSnapshotRepository()
        {
            Model = new ShipSnapshotCatalogModel();

            Refresh();
        }

        public ShipSnapshotCatalogModel Model { get; }

        public void DeleteSnapshot(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Snapshot file path is required.", nameof(filePath));

            File.Delete(filePath);

            Refresh();
        }

        public void SaveSnapshot(
            ShipSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            var filePath = Path.Combine(Constants.ShipSnapshotsFolder, $"{snapshot.shipName}.json");

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Snapshot file path is required.", nameof(filePath));

            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(filePath, json);

            Refresh();
        }

        private void Refresh()
        {
            Model.ReplaceAll(LoadSnapshotsFromDisk());
        }

        private static IReadOnlyList<SavedShipSnapshotDescriptor> LoadSnapshotsFromDisk()
        {
            if (!Directory.Exists(Constants.ShipSnapshotsFolder))
                return Array.Empty<SavedShipSnapshotDescriptor>();

            var snapshotPaths = Directory.GetFiles(Constants.ShipSnapshotsFolder, "*.json");
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
    }
}