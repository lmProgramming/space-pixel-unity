using System.Collections.Generic;
using System.Linq;
using ContourTracer;
using Other;
using Pixelation.CollisionResolver;
using UnityEngine;

namespace Pixelation
{
    public sealed class PixelCollisionHandler
    {
        private const int MinPixelsForJunkCreation = 3;
        private const float DefaultExplosionChange = 0.5f;
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
            var regions = pixels.Count == 1
                ? GridRegionFinder.FloodFindCohesiveRegions(pixels[0], _grid)
                : GridRegionFinder.FloodFindCohesiveRegions(_grid);

            regions = regions.OrderBy(r => r.Count).ToList();

            switch (regions.Count)
            {
                case 0:
                    _body.NoPixelsLeft();
                    return;
                case > 1:
                    HandleDivision(regions.SkipLast(1).ToList());
                    break;
            }

            RecalculateMass(regions[^1].Count);

            RecalculateColliders();
        }

        private void RecalculateMass(int pixelsCount)
        {
            _body.Rigidbody.mass = pixelsCount;
        }

        private void ResolveCollision(IPixelated other, Collision2D collision)
        {
            _didCollide = true;
            var pixelsDestroyed = _collisionResolver.ResolveCollision(other, collision);

            var vector2Ints = pixelsDestroyed as Vector2Int[] ?? pixelsDestroyed.ToArray();
            EffectsOnPixelsDestroyed(vector2Ints);
        }

        private void EffectsOnPixelsDestroyed(Vector2Int[] pixels)
        {
            foreach (var pixel in pixels)
                if (Random.value < DefaultExplosionChange)
                    EffectsSpawner.Instance.SpawnExplosion(_body.LocalToWorldPoint(pixel));
        }

        private void RecalculateColliders()
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
            foreach (var region in regions)
            {
                if (region.Count >= MinPixelsForJunkCreation) CreateNewJunk(region);

                _grid.RemovePixels(region);
            }
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

        public Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
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

            while (radiusChecked < maxRadiusChecked && closestPointsAndDistances.Count < positionsMaxCount)
            {
                closestPointsAndDistances = new List<(Vector2Int Position, float Distance)>();
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

            return closestPointsAndDistances.Select(p => p.Position).Take(positionsMaxCount).ToList();

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