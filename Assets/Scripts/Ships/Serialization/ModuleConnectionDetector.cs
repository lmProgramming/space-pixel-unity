using System.Collections.Generic;
using Core.Ship;
using Pixelation;

namespace Ships.Serialization
{
    public static class ModuleConnectionDetector
    {
        public static void DetectAndCaptureConnections(
            ShipSnapshot snapshot,
            IModule[] modules,
            Dictionary<IModule, int> moduleToIndex)
        {
            for (var i = 0; i < modules.Length - 1; i++)
            for (var j = i + 1; j < modules.Length; j++)
            {
                var moduleA = modules[i];
                var moduleB = modules[j];

                if (moduleA.PixelatedRigidbody == null || moduleB.PixelatedRigidbody == null)
                    continue;

                var pointsA = OverlapCalculator.CalculateOverlappingPoints(
                    moduleA.PixelatedRigidbody,
                    moduleB.PixelatedRigidbody);

                if (pointsA == null || pointsA.Count == 0)
                    continue;

                var pointsB = OverlapCalculator.CalculateOverlappingPoints(
                    moduleB.PixelatedRigidbody,
                    moduleA.PixelatedRigidbody);

                var indexA = moduleToIndex[moduleA];
                var indexB = moduleToIndex[moduleB];

                var connection = new ModuleConnection(indexA, indexB);

                connection.connectionPointsA.AddRange(pointsA);

                if (pointsB != null)
                    connection.connectionPointsB.AddRange(pointsB);

                snapshot.connections.Add(connection);
            }
        }
    }
}