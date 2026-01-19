using System.Collections.Generic;
using Core.Ship;
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

                if (ship.CommandModule != null && module == (Module)ship.CommandModule) snapshot.commandModuleIndex = i;
            }

            CaptureConnections(ship, snapshot, modules, moduleToIndex);

            Debug.Log(
                $"[ShipSnapshotService] Captured snapshot of '{ship.name}' with {snapshot.modules.Count} modules and {snapshot.connections.Count} connections");

            return snapshot;
        }

        public string ToJson(ShipSnapshot snapshot, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(snapshot, prettyPrint);
        }

        public ShipSnapshot FromJson(string json)
        {
            return JsonUtility.FromJson<ShipSnapshot>(json);
        }

        private ModuleSnapshot CaptureModuleSnapshot(Module module)
        {
            var moduleSnapshot = new ModuleSnapshot(module.name, module.Type)
            {
                localPosition = module.transform.localPosition,
                localRotation = module.transform.localRotation,
                localScale = module.transform.localScale
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

        private static void CaptureConnections(
            Ship ship,
            ShipSnapshot snapshot,
            Module[] modules,
            Dictionary<Module, int> moduleToIndex)
        {
            var graph = ship.ModuleGraph;
            var processedPairs = new HashSet<(int, int)>();

            foreach (var moduleA in modules)
            {
                if (!moduleToIndex.TryGetValue(moduleA, out var indexA))
                    continue;

                IModule iModuleA = moduleA;
                var connectedNodes = graph.GetConnectedNodes(iModuleA);

                foreach (var connectedNode in connectedNodes)
                {
                    if (connectedNode is not Module moduleB)
                        continue;

                    if (!moduleToIndex.TryGetValue(moduleB, out var indexB))
                        continue;

                    var pairKey = indexA < indexB ? (indexA, indexB) : (indexB, indexA);
                    if (!processedPairs.Add(pairKey))
                        continue;

                    var connection = new ModuleConnection(indexA, indexB);

                    if (moduleA.ConnectionPoints.TryGetValue(moduleB, out var pointsA))
                        connection.connectionPointsA.AddRange(pointsA);

                    if (moduleB.ConnectionPoints.TryGetValue(moduleA, out var pointsB))
                        connection.connectionPointsB.AddRange(pointsB);

                    snapshot.connections.Add(connection);
                }
            }
        }
    }
}