using System;
using System.Collections.Generic;
using Core.Services;
using External.FyiurAmron;
using UnityEngine;
using ZLinq;

namespace Gameplay.Navigation
{
    public class NavigationCalculator
    {
        private readonly Func<Vector2, SectorResult> _getSectorResult;
        private readonly float _sectorSize;

        public NavigationCalculator(float sectorSize, Func<Vector2, SectorResult> getSectorResult)
        {
            _sectorSize = sectorSize;
            _getSectorResult = getSectorResult;
        }

        public List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize)
        {
            var startSector = NormalizePositionToSector(start);
            var endSector = NormalizePositionToSector(end);

            if (startSector == endSector) return new List<Vector3> { GetSectorCenter(endSector) };

            var footprintRadius = Mathf.CeilToInt(shipSize / _sectorSize);

            var openSet = new PriorityQueue<Vector2, float>();
            var cameFrom = new Dictionary<Vector2, Vector2>();
            var gScore = new Dictionary<Vector2, float>
            {
                [startSector] = 0f
            };

            openSet.Enqueue(startSector, Heuristic(startSector, endSector, startSector));

            var iterations = 0;
            const int maxIterations = 1000;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;
                var current = openSet.Dequeue();

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (Vector2.Distance(neighbor, endSector) < 0.1f)
                        return ReconstructPath(cameFrom, current);

                    if (!IsSectorNavigable(neighbor, footprintRadius))
                        continue;

                    var distanceToNeighbor = Vector2.Distance(current, neighbor);
                    var tentativeG = gScore[current] + distanceToNeighbor;

                    if (gScore.TryGetValue(neighbor, out var existingG) && tentativeG >= existingG - 0.001f)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    var fScore = tentativeG + Heuristic(neighbor, endSector, startSector);
                    openSet.Enqueue(neighbor, fScore);
                }
            }

            return null;
        }

        public Vector2 NormalizePositionToSector(Vector3 position)
        {
            return new Vector2(
                Mathf.Floor(position.x / _sectorSize) * _sectorSize,
                Mathf.Floor(position.y / _sectorSize) * _sectorSize);
        }

        public Vector2 GetSectorCenter(Vector2 sector)
        {
            return sector + new Vector2(_sectorSize * 0.5f, _sectorSize * 0.5f);
        }

        private IEnumerable<Vector2> GetNeighbors(Vector2 sector)
        {
            yield return new Vector2(sector.x + _sectorSize, sector.y);
            yield return new Vector2(sector.x - _sectorSize, sector.y);
            yield return new Vector2(sector.x, sector.y + _sectorSize);
            yield return new Vector2(sector.x, sector.y - _sectorSize);
        }

        private bool IsSectorNavigable(Vector2 centerSector, int radius)
        {
            var radiusNormalized = radius - 1;
            for (var x = -radiusNormalized; x <= radiusNormalized; x++)
            for (var y = -radiusNormalized; y <= radiusNormalized; y++)
            {
                var checkSector = new Vector2(centerSector.x + x * _sectorSize, centerSector.y + y * _sectorSize);
                if (!_getSectorResult(checkSector).Empty)
                    return false;
            }

            return true;
        }

        private static float Heuristic(Vector2 current, Vector2 end, Vector2 start)
        {
            var manhattan = Mathf.Abs(current.x - end.x) + Mathf.Abs(current.y - end.y);

            // Cross-product tie breaker
            var dx1 = current.x - end.x;
            var dy1 = current.y - end.y;
            var dx2 = start.x - end.x;
            var dy2 = start.y - end.y;
            var crossProduct = Mathf.Abs(dx1 * dy2 - dx2 * dy1);

            // Add a tiny penalty for veering off the straight line
            return manhattan + crossProduct * 0.001f;
        }

        private List<Vector3> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 current)
        {
            var path = new List<Vector3> { GetSectorCenter(current) };

            while (cameFrom.TryGetValue(current, out var prev))
            {
                if (prev == current) break;
                current = prev;
                path.Add(GetSectorCenter(current));
            }

            path.Reverse();
            return path.AsValueEnumerable().Skip(1).ToList();
        }
    }
}