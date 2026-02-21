using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Services;
using External.FyiurAmron;
using UnityEngine;
using ZLinq;

[assembly: InternalsVisibleTo("Game.Editor")]

namespace Services
{
    public class SectorService : MonoBehaviour, ISectorService
    {
        [SerializeField] private float sectorSize = 10f;
        [SerializeField] private float cacheDuration = 1f;

        private readonly Collider2D[] _results = new Collider2D[32];

        private readonly Dictionary<Vector2, SectorResult> _sectorCache = new();
        private ContactFilter2D _filter;
        private Vector2 Sector => new(sectorSize, sectorSize);

        private void Awake()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = ~LayerMask.GetMask("Enemy", "Friendly"),
                useTriggers = false
            };
        }

        public SectorResult GetSectorResult(Vector3 position)
        {
            if (_sectorCache.TryGetValue(NormalizePositionToSector(position), out var cachedResult) &&
                cachedResult.GenerationTime > Time.time - cacheDuration) return cachedResult;

            var normalizedPosition = NormalizePositionToSector(position);

            var count = Physics2D.OverlapBox(
                normalizedPosition,
                Sector,
                0,
                _filter,
                _results
            );

            var empty = count == 0;

            var result = new SectorResult(empty, Time.time);
            _sectorCache[normalizedPosition] = result;

            return result;
        }

        public List<Vector3> CalculatePath(Vector3 start, Vector3 end, int shipSize)
        {
            var startSector = NormalizePositionToSector(start);
            var endSector = NormalizePositionToSector(end);

            if (startSector == endSector) return new List<Vector3> { GetSectorCenter(endSector) };

            var footprintRadius = Mathf.CeilToInt(shipSize / sectorSize / 2f);

            var openSet = new PriorityQueue<Vector2, float>();
            var cameFrom = new Dictionary<Vector2, Vector2>();
            var gScore = new Dictionary<Vector2, float>
            {
                [startSector] = 0f
            };

            openSet.Enqueue(startSector, Heuristic(startSector, endSector));

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

                    var tentativeG = gScore[current] + sectorSize;

                    if (gScore.TryGetValue(neighbor, out var existingG) && tentativeG >= existingG - 0.001f)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    var fScore = tentativeG + Heuristic(neighbor, endSector);
                    openSet.Enqueue(neighbor, fScore);
                }
            }

            return null;
        }

        public void ClearCacheEntries(IEnumerable<Vector2> keys)
        {
            foreach (var key in keys)
                _sectorCache.Remove(key);
        }

        private IEnumerable<Vector2> GetNeighbors(Vector2 sector)
        {
            var s = sectorSize;

            yield return new Vector2(sector.x + s, sector.y);
            yield return new Vector2(sector.x - s, sector.y);
            yield return new Vector2(sector.x, sector.y + s);
            yield return new Vector2(sector.x, sector.y - s);
        }

        private bool IsSectorNavigable(Vector2 centerSector, int radius)
        {
            var radiusNormalized = radius - 1;
            for (var x = -radiusNormalized; x <= radiusNormalized; x++)
            for (var y = -radiusNormalized; y <= radiusNormalized; y++)
            {
                var checkSector = new Vector2(centerSector.x + x * sectorSize, centerSector.y + y * sectorSize);
                if (!GetSectorResult(checkSector).Empty)
                    return false;
            }

            return true;
        }

        private static float Heuristic(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b);
        }

        private List<Vector3> ReconstructPath(
            Dictionary<Vector2, Vector2> cameFrom,
            Vector2 current)
        {
            var path = new List<Vector3> { GetSectorCenter(current) };

            while (cameFrom.TryGetValue(current, out var prev))
            {
                if (prev == current) break; // Safety
                current = prev;
                path.Add(GetSectorCenter(current));
            }

            path.Reverse();
            return path.AsValueEnumerable().Skip(1).ToList();
        }

        private Vector2 GetSectorCenter(Vector2 sector)
        {
            return sector + new Vector2(sectorSize * 0.5f, sectorSize * 0.5f);
        }

        private Vector2 NormalizePositionToSector(Vector3 position)
        {
            return new Vector2(
                Mathf.Floor(position.x / sectorSize) * sectorSize,
                Mathf.Floor(position.y / sectorSize) * sectorSize);
        }

#if UNITY_EDITOR
        internal float InternalSectorSize => sectorSize;
        internal float InternalCacheDuration => cacheDuration;
        internal IReadOnlyDictionary<Vector2, SectorResult> InternalCache => _sectorCache;
#endif
    }
}