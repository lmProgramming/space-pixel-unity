using System.Collections.Generic;
using System.Linq;
using ContourTracer;
using Pixelation.CollisionResolver;
using UnityEngine;

namespace Pixelation
{
    public class PixelCollisionHandler
    {
        private const int MinPixelsForJunkCreation = 3;
        private readonly PixelatedRigidbody _body;
        private readonly PolygonCollider2D _collider;
        private readonly CollisionResolver.CollisionResolver _collisionResolver;
        private readonly PixelGrid _grid;
        private readonly GridContourTracer _gridContourTracer = new();
        private readonly float _lineSimplificationTolerance;

        private bool _didCollide;

        public PixelCollisionHandler(PixelGrid grid, PixelatedRigidbody body, PolygonCollider2D collider)
        {
            _grid = grid;
            _body = body;
            _collider = collider;

            _collisionResolver = new PhysicsCollision(this, _body);

            body.OnPixelsDestroyed += PixelsDestroyed;
        }

        private void PixelsDestroyed(List<Vector2Int> pixels)
        {
            var regions = FloodFindCohesiveRegions(pixels[0]);

            if (regions.Count > 1) HandleDivision(regions);

            RecalculateColliders();
        }

        public void ResolveCollision(IPixelated other, Collision2D collision)
        {
            _didCollide = true;
            _collisionResolver.ResolveCollision(other, collision);
        }

        public void RecalculateColliders()
        {
            var polygon = _gridContourTracer.GenerateCollider(_grid.Texture, new Vector2(.5f, .5f), 1);
            if (polygon is null)
            {
                _body.NoPixelsLeft();
                return;
            }

            var points = new List<Vector2>();

            LineUtility.Simplify(polygon.ToList(), _lineSimplificationTolerance, points);

            _collider.pathCount = 1;
            _collider.SetPath(0, points);
        }

        private void HandleDivision(List<HashSet<Vector2Int>> regions)
        {
            regions = regions.OrderBy(r => r.Count).ToList();

            for (var index = 0; index < regions.Count - 1; index++)
            {
                var region = regions[index];

                if (region.Count >= MinPixelsForJunkCreation) CreateNewJunk(region);

                _grid.RemovePixels(region);
            }

            RecalculateColliders();
        }

        private void CreateNewJunk(HashSet<Vector2Int> points)
        {
            var rightTopPoint = new Vector2Int(points.Max(p => p.x), points.Max(p => p.y));
            var leftBottomPoint = new Vector2Int(points.Min(p => p.x), points.Min(p => p.y));
            var parentCenterPoint = _grid.Center;

            var width = rightTopPoint.x - leftBottomPoint.x + 1;
            var height = rightTopPoint.y - leftBottomPoint.y + 1;

            var centrePoint = leftBottomPoint + new Vector2(width, height) / 2;

            var newColorsGrid = new Color32[width, height];

            foreach (var point in points)
                newColorsGrid[point.x - leftBottomPoint.x, point.y - leftBottomPoint.y] = _grid.GetColor(point);

            var globalPosition = _body.transform.TransformPoint(centrePoint - parentCenterPoint);

            JunkSpawner.Instance.SpawnJunk(globalPosition, _body.transform.rotation, newColorsGrid, _body);
        }

        private List<HashSet<Vector2Int>> FloodFindCohesiveRegions(Vector2Int searchStartPoint)
        {
            var visited = new HashSet<Vector2Int>
            {
                searchStartPoint
            };

            var regions = new List<HashSet<Vector2Int>>();

            SetupFlooding(searchStartPoint + new Vector2Int(1, 0));
            SetupFlooding(searchStartPoint + new Vector2Int(-1, 0));
            SetupFlooding(searchStartPoint + new Vector2Int(0, 1));
            SetupFlooding(searchStartPoint + new Vector2Int(0, -1));

            return regions;

            void SetupFlooding(Vector2Int searchStart)
            {
                if (!_grid.InBounds(searchStart)) return;

                regions.Add(new HashSet<Vector2Int> { searchStart });

                FloodFind(searchStart, regions.Count - 1);
            }

            void FloodFind(Vector2Int position, int regionIndex)
            {
                while (true)
                {
                    if (regionIndex == regions.Count) return;

                    if (!_grid.IsPixel(position)) return;

                    if (!visited.Add(position))
                    {
                        FindRegionToMerge(position, regionIndex);
                        return;
                    }

                    regions[regionIndex].Add(position);

                    FloodFind(position + new Vector2Int(1, 0), regionIndex);
                    FloodFind(position + new Vector2Int(-1, 0), regionIndex);
                    FloodFind(position + new Vector2Int(0, 1), regionIndex);
                    // recursion changed to tail
                    position += new Vector2Int(0, -1);
                }
            }

            void FindRegionToMerge(Vector2Int position, int regionIndex)
            {
                for (var index = 0; index < regions.Count; index++)
                {
                    var region = regions[index];

                    if (!region.Contains(position)) continue;

                    if (index == regionIndex) break;

                    MergeRegions(index, regionIndex);
                }
            }

            void MergeRegions(int indexToMergeWith, int indexMerged)
            {
                regions[indexToMergeWith].UnionWith(regions[indexMerged]);

                regions.RemoveAt(indexMerged);
            }
        }

        private Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
        {
            var pointsTraversed = GridMarcher.March(new Vector2Int(_grid.Width, _grid.Height),
                startPosition,
                direction);

            if (getLast) pointsTraversed.Reverse();

            foreach (var point in pointsTraversed.Where(_grid.IsPixel))
                return new Vector2Int(point.x, point.y);

            return null;
        }

        public List<Vector2Int> GetClosestPixelPositions(Vector2 localPosition, int positionsMaxCount)
        {
            var localPositionInt = new Vector2Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y));

            var radiusChecked = 0;

            var maxRadiusChecked = Mathf.Max(_grid.Width, _grid.Height);

            var closestPointsAndDistances = new List<(Vector2Int Position, float Distance)>();

            while (radiusChecked < maxRadiusChecked)
            {
                if (closestPointsAndDistances.Count >= positionsMaxCount) break;

                for (var x = localPositionInt.x - radiusChecked; x <= localPositionInt.x + radiusChecked; x++)
                for (var y = localPositionInt.y - radiusChecked; y <= localPositionInt.y + radiusChecked; y++)
                {
                    var pixelPosition = new Vector2Int(x, y);

                    if (!_grid.IsPixel(pixelPosition)) continue;

                    var distance = (new Vector2(x, y) - localPosition).SqrMagnitude();

                    InsertPositionToSortedArray(pixelPosition, distance);
                }

                radiusChecked++;
            }

            return closestPointsAndDistances.Select(p => p.Position).ToList();

            void InsertPositionToSortedArray(Vector2Int position, float distance)
            {
                for (var index = 0; index < closestPointsAndDistances.Count; index++)
                {
                    var closestPointAndDistance = closestPointsAndDistances[index];

                    if (!(distance < closestPointAndDistance.Distance)) continue;
                    closestPointsAndDistances.Insert(index, (position, radiusChecked));
                    return;
                }

                closestPointsAndDistances.Add((position, radiusChecked));
            }
        }

        public Vector2Int? GetClosestPixelPosition(Vector2 localPosition)
        {
            var positions = GetClosestPixelPositions(localPosition, 1);

            if (positions.Count > 0) return positions[0];
            return null;
        }

        public void SetCollided(bool isCollided)
        {
            _didCollide = isCollided;
        }

        public void OnCollision(Collision2D collision)
        {
            if (_didCollide) return;

            var otherRb = collision.gameObject.GetComponent<PixelatedRigidbody>();

            if (otherRb is null) return;

            otherRb.CollisionHandler.ResolveCollision(_body, collision);

            ResolveCollision(otherRb, collision);
        }
    }
}