using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;

namespace Ships.Serialization
{
    public class ShipSnapshotService : IShipSnapshotService
    {
        public ShipSnapshot CaptureSnapshot(Ship ship)
        {
            if (!ship)
            {
                Debug.LogError("[ShipSnapshotService] Cannot capture snapshot: ship is null");
                return null;
            }

            var snapshot = new ShipSnapshot(ship.name);
            var modules = ship.GetComponentsInChildren<Module>();
            var moduleToIndex = new Dictionary<Module, int>();

            for (var i = 0; i < modules.Length; i++)
            {
                var module = modules[i];
                moduleToIndex[module] = i;

                var moduleSnapshot = CaptureModuleSnapshot(module);
                snapshot.modules.Add(moduleSnapshot);

                if (ship.CommandModule != null && module == (Module)ship.CommandModule)
                    snapshot.commandModuleIndex = i;
            }

            ModuleConnectionDetector.DetectAndCaptureConnections(snapshot, modules, moduleToIndex);

            Debug.Log(
                $"[ShipSnapshotService] Captured snapshot of '{ship.name}' with {snapshot.modules.Count} modules and {snapshot.connections.Count} connections");

            return snapshot;
        }

        public void ApplySnapshot(Ship ship, ShipSnapshot snapshot)
        {
            if (!ship)
            {
                Debug.LogError("[ShipSnapshotService] Cannot apply snapshot: ship is null");
                return;
            }

            if (snapshot == null)
            {
                Debug.LogError("[ShipSnapshotService] Cannot apply snapshot: snapshot is null");
                return;
            }

            var modules = ship.GetComponentsInChildren<Module>();

            if (modules.Length != snapshot.modules.Count)
                Debug.LogWarning(
                    $"[ShipSnapshotService] Module count mismatch: ship has {modules.Length}, snapshot has {snapshot.modules.Count}. Applying by index.");

            var count = Mathf.Min(modules.Length, snapshot.modules.Count);

            for (var i = 0; i < count; i++)
                ApplyModuleSnapshot(modules[i], snapshot.modules[i]);

            Debug.Log(
                $"[ShipSnapshotService] Applied snapshot '{snapshot.shipName}' to '{ship.name}' ({count} modules)");
        }

        public string ToJson(ShipSnapshot snapshot, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(snapshot, prettyPrint);
        }

        public ShipSnapshot FromJson(string json)
        {
            return JsonUtility.FromJson<ShipSnapshot>(json);
        }

        private static ModuleSnapshot CaptureModuleSnapshot(Module module)
        {
            var moduleSnapshot = new ModuleSnapshot(module.name, module.Type)
            {
                localPosition = module.transform.localPosition,
                localRotation = module.transform.localRotation
            };

            var pixelatedRb = module.PixelatedRigidbody;

            if (pixelatedRb?.PixelGrid == null) return moduleSnapshot;

            var grid = pixelatedRb.PixelGrid;
            var dimensions = grid.Dimensions();
            moduleSnapshot.pixelGrid = new PixelGridSnapshot(dimensions.x, dimensions.y);

            for (var y = 0; y < dimensions.y; y++)
            for (var x = 0; x < dimensions.x; x++)
            {
                var pos = new Vector2Int(x, y);
                if (grid.IsPixel(pos)) moduleSnapshot.pixelGrid.SetPixel(x, y, grid.GetColor(pos));
            }

            return moduleSnapshot;
        }

        private static void ApplyModuleSnapshot(Module module, ModuleSnapshot moduleSnapshot)
        {
            module.transform.localPosition = moduleSnapshot.localPosition;
            module.transform.localRotation = moduleSnapshot.localRotation;

            if (moduleSnapshot.pixelGrid == null) return;

            var pixelatedRb = module.PixelatedRigidbody;
            if (pixelatedRb == null) return;

            var pg = moduleSnapshot.pixelGrid;
            var colors = new Color32[pg.width, pg.height];

            for (var y = 0; y < pg.height; y++)
            for (var x = 0; x < pg.width; x++)
                colors[x, y] = pg.GetPixel(x, y);

            pixelatedRb.SetTextureFromColors(colors);
        }
    }
}