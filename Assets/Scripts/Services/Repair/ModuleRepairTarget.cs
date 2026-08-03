using System;
using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Services.Repair
{
    public class ModuleRepairTarget
    {
        private static readonly Vector2Int[] EightNeighborOffsets =
        {
            new(1, 0), new(1, 1), new(0, 1), new(-1, 1),
            new(-1, 0), new(-1, -1), new(0, -1), new(1, -1)
        };

        private readonly Dictionary<int, List<Vector2Int>> _buckets = new();
        private readonly HashSet<Vector2Int> _candidates = new();
        private readonly Color32[,] _pristineColors;
        private readonly float[,] _pristineHealth;

        public ModuleRepairTarget(IPixelatedRigidbody body, Color32[,] pristineColors, float[,] pristineHealth)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            _pristineColors = pristineColors ?? throw new ArgumentNullException(nameof(pristineColors));
            _pristineHealth = pristineHealth ?? throw new ArgumentNullException(nameof(pristineHealth));

            var width = _pristineColors.GetLength(0);
            var height = _pristineColors.GetLength(1);
            if (_pristineHealth.GetLength(0) != width || _pristineHealth.GetLength(1) != height)
                throw new ArgumentException("[ModuleRepairTarget] Pristine health dimensions do not match colors.");

            RebuildCandidates();
        }

        public IPixelatedRigidbody Body { get; }

        public bool HasWorkRemaining => _candidates.Count > 0;
        public int RemainingPixelCount => _candidates.Count;

        public void RebuildCandidates()
        {
            _candidates.Clear();
            _buckets.Clear();

            var width = _pristineColors.GetLength(0);
            var height = _pristineColors.GetLength(1);

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2Int(x, y);
                if (_pristineColors[x, y].a == 0) continue;
                if (Body.IsPixel(point)) continue;
                if (CountExistingNeighbors(point) == 0) continue;

                _candidates.Add(point);
                AddToBucket(point, CountExistingNeighbors(point));
            }
        }

        public bool TryPeekNext(out Vector2Int point)
        {
            for (var neighborCount = 8; neighborCount >= 1; neighborCount--)
            {
                if (!_buckets.TryGetValue(neighborCount, out var bucket) || bucket.Count == 0)
                    continue;

                point = PickClosestToCenter(bucket);
                return true;
            }

            point = default;
            return false;
        }

        public Pixel RestoreNextPixel()
        {
            if (!TryPeekNext(out var point))
                throw new InvalidOperationException("[ModuleRepairTarget] No repair candidates remaining.");

            RemoveFromBuckets(point);
            _candidates.Remove(point);

            var restored = new Pixel(point, _pristineColors[point.x, point.y],
                _pristineHealth[point.x, point.y]);
            Body.RestorePixels(new[] { restored });

            RefreshNeighborsOf(point);
            return restored;
        }

        private void RefreshNeighborsOf(Vector2Int restoredPoint)
        {
            var width = _pristineColors.GetLength(0);
            var height = _pristineColors.GetLength(1);

            foreach (var offset in EightNeighborOffsets)
            {
                var neighbor = restoredPoint + offset;
                if (neighbor.x < 0 || neighbor.y < 0 || neighbor.x >= width || neighbor.y >= height)
                    continue;
                if (_pristineColors[neighbor.x, neighbor.y].a == 0) continue;
                if (Body.IsPixel(neighbor)) continue;

                var neighborCount = CountExistingNeighbors(neighbor);
                if (neighborCount == 0)
                {
                    if (_candidates.Remove(neighbor))
                        RemoveFromBuckets(neighbor);
                    continue;
                }

                if (_candidates.Add(neighbor))
                {
                    AddToBucket(neighbor, neighborCount);
                    continue;
                }

                RemoveFromBuckets(neighbor);
                AddToBucket(neighbor, neighborCount);
            }
        }

        private int CountExistingNeighbors(Vector2Int point)
        {
            var count = 0;
            foreach (var offset in EightNeighborOffsets)
            {
                var neighbor = point + offset;
                if (Body.IsPixel(neighbor))
                    count++;
            }

            return count;
        }

        private void AddToBucket(Vector2Int point, int neighborCount)
        {
            if (!_buckets.TryGetValue(neighborCount, out var bucket))
            {
                bucket = new List<Vector2Int>();
                _buckets[neighborCount] = bucket;
            }

            bucket.Add(point);
        }

        private void RemoveFromBuckets(Vector2Int point)
        {
            foreach (var bucket in _buckets.Values)
                bucket.Remove(point);
        }

        private Vector2Int PickClosestToCenter(List<Vector2Int> bucket)
        {
            var center = Body.WeightedCenter;
            var best = bucket[0];
            var bestDistance = (new Vector2(best.x, best.y) - center).sqrMagnitude;

            for (var i = 1; i < bucket.Count; i++)
            {
                var candidate = bucket[i];
                var distance = (new Vector2(candidate.x, candidate.y) - center).sqrMagnitude;
                if (!(distance < bestDistance)) continue;
                bestDistance = distance;
                best = candidate;
            }

            return best;
        }
    }
}