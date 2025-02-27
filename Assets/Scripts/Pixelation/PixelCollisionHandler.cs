using System.Collections.Generic;
using ContourTracer;
using LM;
using Pixelation.CollisionResolver;
using UnityEngine;
using Enumerable = System.Linq.Enumerable;

namespace Pixelation
{
    public sealed class PixelCollisionHandler
    {
        private const int MinPixelsForJunkCreation = 3;
        private const float DefaultExplosionChange = 0.25f;
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

            regions = Enumerable.ToList(Enumerable.OrderBy(regions, r => r.Count));

            switch (regions.Count)
            {
                case 0:
                    _body.NoPixelsLeft();
                    return;
                case > 1:
                    HandleDivision(Enumerable.ToList(Enumerable.SkipLast(regions, 1)));
                    break;
            }

            RecalculateMass(regions[^1].Count);

            RecalculateColliders();
        }

        private void RecalculateMass(int pixelsCount)
        {
            _body.Rigidbody.mass = pixelsCount;
        }

        private void ResolveCollision(PixelatedRigidbody other, Collision2D collision)
        {
            _didCollide = true;
            var pixelsDestroyed = _collisionResolver.ResolveCollision(other, collision);

            var vector2Ints = pixelsDestroyed as Vector2Int[] ?? Enumerable.ToArray(pixelsDestroyed);
            EffectsOnPixelsDestroyed(vector2Ints);
        }

        private void EffectsOnPixelsDestroyed(Vector2Int[] pixels)
        {
            var explosionsCount = Mathf.Min(pixels.Length - 1, Mathf.Max(1, pixels.Length * DefaultExplosionChange));
            for (var index = 0; index < explosionsCount; index++)
                EffectsSpawner.Instance.SpawnExplosion(_body.LocalToWorldPoint(pixels[index]));
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

            LineUtility.Simplify(Enumerable.ToList(polygon), _lineSimplificationTolerance, points);

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
            var rightTopPoint = new Vector2Int(Enumerable.Max(points, p => p.x), Enumerable.Max(points, p => p.y));
            var leftBottomPoint = new Vector2Int(Enumerable.Min(points, p => p.x), Enumerable.Min(points, p => p.y));
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

            foreach (var point in Enumerable.Where(pointsTraversed, _grid.IsPixel))
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

            return Enumerable.ToList(Enumerable.Take(Enumerable.Select(closestPointsAndDistances, p => p.Position),
                positionsMaxCount));

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