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
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ShipSnapshotRepository : IShipSnapshotRepository
    {
        private readonly List<Sprite> _loadedIcons = new();

        // The skirmish "Friend" asset is the built-in starter ship ("Ally") shipped with the game.
        private const string StarterShipResourcePath = "ShipSnapshots/Friend";

        public ShipSnapshotRepository()
        {
            Model = new ShipSnapshotCatalogModel();

            EnsureStarterShipSeeded();
            Refresh();
        }

        public ShipSnapshotCatalogModel Model { get; }

        public bool SnapshotExists(string shipName)
        {
            if (string.IsNullOrWhiteSpace(shipName) ||
                shipName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Snapshot ship name is invalid.", nameof(shipName));

            var filePath = FilePathForShipName(shipName);

            return File.Exists(filePath);
        }

        public void DeleteSnapshot(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Snapshot file path is required.", nameof(filePath));

            File.Delete(filePath);

            var shipName = Path.GetFileNameWithoutExtension(filePath);
            DeleteIconForSnapshot(shipName);

            Refresh();
        }

        public void SaveSnapshot(
            ShipSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (string.IsNullOrWhiteSpace(snapshot.shipName) ||
                snapshot.shipName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Snapshot ship name is invalid.", nameof(snapshot));

            var filePath = FilePathForShipName(snapshot.shipName);

            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(filePath, json);

            SaveIconForSnapshot(snapshot);

            Refresh();
        }

        private static void DeleteIconForSnapshot(string shipName)
        {
            var iconPath = GetShipSnapshotIconPath(shipName);
            if (File.Exists(iconPath))
                File.Delete(iconPath);
        }

        private static string FilePathForShipName(string shipName)
        {
            return Path.Combine(Constants.ShipSnapshotsFolder, $"{shipName}{Constants.ShipSnapshotExtension}");
        }

        private static string GetShipSnapshotIconPath(string shipName)
        {
            return Path.Combine(Constants.ShipSnapshotsFolder, $"{shipName}{Constants.ShipSnapshotIconExtension}");
        }

        private static void EnsureStarterShipSeeded()
        {
            if (Directory.Exists(Constants.ShipSnapshotsFolder))
                return;

            var starterShipJson = Resources.Load<TextAsset>(StarterShipResourcePath);
            if (starterShipJson == null)
                throw new UnityException(
                    $"[ShipSnapshotRepository] Starter ship resource '{StarterShipResourcePath}' is missing.");

            var starterSnapshot = JsonUtility.FromJson<ShipSnapshot>(starterShipJson.text);
            if (starterSnapshot == null || string.IsNullOrWhiteSpace(starterSnapshot.shipName))
                throw new UnityException(
                    "[ShipSnapshotRepository] Starter ship resource does not contain a valid ship snapshot.");

            var filePath = FilePathForShipName(starterSnapshot.shipName);
            var directoryPath = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(filePath, starterShipJson.text);
        }

        private void Refresh()
        {
            ClearLoadedIcons();
            Model.ReplaceAll(LoadSnapshotsFromDisk());
        }

        private void ClearLoadedIcons()
        {
            foreach (var icon in _loadedIcons)
                ShipPreviewIconCompositor.DestroySprite(icon);

            _loadedIcons.Clear();
        }

        private static void SaveIconForSnapshot(ShipSnapshot snapshot)
        {
            var iconPath = GetShipSnapshotIconPath(snapshot.shipName);
            var iconSprite = ShipPreviewIconCompositor.ComposeFromSnapshot(snapshot);

            if (iconSprite != null)
            {
                ShipPreviewIconCompositor.SavePng(iconSprite.texture, iconPath);
                ShipPreviewIconCompositor.DestroySprite(iconSprite);
                return;
            }

            if (File.Exists(iconPath))
                File.Delete(iconPath);
        }

        private IReadOnlyList<SavedShipSnapshotDescriptor> LoadSnapshotsFromDisk()
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
                    snapshotPath,
                    LoadIconForSnapshot(snapshotPath));
            }

            return descriptors;
        }

        private Sprite CreateIconForSnapshot(string snapshotPath, string iconPath)
        {
            var json = File.ReadAllText(snapshotPath);
            var snapshot = JsonUtility.FromJson<ShipSnapshot>(json);
            var composedIcon = ShipPreviewIconCompositor.ComposeFromSnapshot(snapshot);
            if (composedIcon == null)
                return null;

            ShipPreviewIconCompositor.SavePng(composedIcon.texture, iconPath);
            _loadedIcons.Add(composedIcon);
            return composedIcon;
        }

        private Sprite LoadIconForSnapshot(string snapshotPath)
        {
            var shipName = Path.GetFileNameWithoutExtension(snapshotPath);
            var iconPath = GetShipSnapshotIconPath(shipName);

            if (!File.Exists(iconPath)) return CreateIconForSnapshot(snapshotPath, iconPath);

            var iconFromDisk = ShipPreviewIconCompositor.LoadSpriteFromPng(iconPath);

            if (iconFromDisk == null) return CreateIconForSnapshot(snapshotPath, iconPath);

            _loadedIcons.Add(iconFromDisk);

            return iconFromDisk;
        }
    }
}