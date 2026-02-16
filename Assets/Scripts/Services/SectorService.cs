using System.Collections.Generic;
using Core.Services;
using External.FyiurAmron;
using UnityEngine;

namespace Services
{
    public class SectorService : MonoBehaviour, ISectorService
    {
        [SerializeField] private float sectorSize = 10f;
        [SerializeField] private float cacheDuration = 1f;

        private readonly Collider2D[] _results = new Collider2D[32];

        private readonly Dictionary<Vector2, SectorResult> _sectorCache = new();
        private ContactFilter2D _filter;

        public float SectorSize => sectorSize;
        public float CacheDuration => cacheDuration;
        public IReadOnlyDictionary<Vector2, SectorResult> Cache => _sectorCache;

        private Vector2 Sector => new(sectorSize, sectorSize);

        private void Awake()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = LayerMask.GetMask("Enemy"),
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

            var footprintRadius = Mathf.CeilToInt(shipSize / sectorSize / 2f);

            var openSet = new PriorityQueue<Vector2, float>();
            var cameFrom = new Dictionary<Vector2, Vector2>();
            var gScore = new Dictionary<Vector2, float>
            {
                [startSector] = 0f
            };

            openSet.Enqueue(startSector, Heuristic(startSector, endSector));

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();

                if (current == endSector)
                    return ReconstructPath(cameFrom, current);

                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!IsSectorWalkable(neighbor, footprintRadius))
                        continue;

                    var tentativeG = gScore[current] + sectorSize;

                    if (gScore.TryGetValue(neighbor, out var existingG) && tentativeG >= existingG)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    var fScore = tentativeG + Heuristic(neighbor, endSector);
                    openSet.Enqueue(neighbor, fScore);
                }
            }

            return null;
        }

        private IEnumerable<Vector2> GetNeighbors(Vector2 sector)
        {
            var s = sectorSize;

            yield return sector + new Vector2(s, 0);
            yield return sector + new Vector2(-s, 0);
            yield return sector + new Vector2(0, s);
            yield return sector + new Vector2(0, -s);
        }

        private bool IsSectorWalkable(Vector2 centerSector, int radius)
        {
            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
            {
                var checkSector = centerSector + new Vector2(x * sectorSize, y * sectorSize);
                if (!GetSectorResult(checkSector).Empty)
                    return false;
            }

            return true;
        }

        private float Heuristic(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private List<Vector3> ReconstructPath(
            Dictionary<Vector2, Vector2> cameFrom,
            Vector2 current)
        {
            var path = new List<Vector3> { current };

            while (cameFrom.TryGetValue(current, out var prev))
            {
                current = prev;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private Vector2 NormalizePositionToSector(Vector3 position)
        {
            return new Vector2(position.x - position.x % sectorSize, position.y - position.y % sectorSize);
        }
    }
}