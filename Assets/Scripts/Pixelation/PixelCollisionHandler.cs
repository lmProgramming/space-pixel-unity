using System.Collections.Generic;
using ContourTracer;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Events.Gameplay.Collision;
using Grid;
using LMPro;
using Pixelation.CollisionResolver;
using UnityEngine;
using ZLinq;

namespace Pixelation
{
    public sealed class PixelCollisionHandler : IPixelCollisionHandler
    {
        private const int MinPixelsForDebrisCreation = 3;
        private readonly PixelatedRigidbody _body;
        private readonly PolygonCollider2D _collider;

        private readonly CollisionEventChannelSO _collisionEventChannel;
        private readonly CollisionResolver.CollisionResolver _collisionResolver;
        private readonly IDebrisSpawner _debrisSpawner;
        private readonly ITexturePixelGrid _grid;
        private readonly GridContourTracer _gridContourTracer = new();
        private readonly float _lineSimplificationTolerance;

        private bool _didCollide;

        public PixelCollisionHandler(ITexturePixelGrid grid, PixelatedRigidbody body, PolygonCollider2D collider,
            CollisionEventChannelSO collisionEventChannel, IDebrisSpawner debrisSpawner)
        {
            _grid = grid;
            _body = body;
            _collider = collider;
            _collisionEventChannel = collisionEventChannel;
            _debrisSpawner = debrisSpawner;

            _collisionResolver = new PhysicsCollision(this, _body);

            body.OnPixelsLost += PixelsLost;
        }

        public void Unsubscribe()
        {
            _body.OnPixelsLost -= PixelsLost;
        }

        public void ForceRecalculateColliders()
        {
            RecalculateColliders();
        }

        public Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
        {
            var pointsTraversed = GridMarcher.March(new Vector2Int(_grid.Width, _grid.Height),
                startPosition,
                direction);

            if (getLast) pointsTraversed.Reverse();

            foreach (var point in pointsTraversed.AsValueEnumerable().Where(_grid.IsPixel))
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

            return closestPointsAndDistances.AsValueEnumerable().Select(p => p.Position).Take(positionsMaxCount)
                .ToList();

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

        public void ResolveCollision(IPixelatedRigidbody other, Collision2D collision)
        {
            _didCollide = true;
            var pixelsDestroyed = _collisionResolver.ResolveCollision(other, collision);

            var pixels = pixelsDestroyed as Vector2Int[] ?? pixelsDestroyed.AsValueEnumerable().ToArray();

            RaiseCollisionEvent(other, collision.contacts[0].point, pixels);
        }

        public void RaiseCollisionEvent(IPixelatedRigidbody other, Vector2 contactPoint,
            Vector2Int[] pixels)
        {
            var pixelsGlobalPositions = new Vector2[pixels.Length];

            for (var i = 0; i < pixels.Length; i++) pixelsGlobalPositions[i] = _body.LocalToWorldPoint(pixels[i]);

            Vector2? speedDifference = null;
            if (other != null) speedDifference = _body.Rigidbody.linearVelocity + other.Rigidbody.linearVelocity;

            var data = new CollisionData(
                _body.gameObject,
                other?.GameObject,
                contactPoint,
                pixelsGlobalPositions,
                speedDifference
            );
            _collisionEventChannel.Raise(data);
        }

        private void PixelsLost(List<Vector2Int> pixels, PixelLoseReason reason)
        {
            if (reason == PixelLoseReason.Division) return;

            var regions = pixels.Count == 1
                ? GridRegionFinder.FloodFindCohesiveRegions(pixels[0], _grid)
                : GridRegionFinder.FloodFindCohesiveRegions(_grid);

            regions = regions.AsValueEnumerable().OrderBy(r => r.Count).ToList();

            switch (regions.Count)
            {
                case 0:
                    _body.NoPixelsLeft();
                    return;
                case > 1:
                    HandleDivision(regions.AsValueEnumerable().SkipLast(1).ToList());
                    break;
            }

            RecalculateMass();

            RecalculateColliders();
        }

        private void RecalculateMass()
        {
            _body.Rigidbody.mass = _grid.PixelCount * _body.MassMultiplier;
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

            LineUtility.Simplify(polygon.AsValueEnumerable().ToList(), _lineSimplificationTolerance, points);

            _collider.pathCount = 1;
            _collider.SetPath(0, points);
        }

        private void HandleDivision(List<HashSet<Vector2Int>> regions)
        {
            foreach (var region in regions)
            {
                if (region.Count >= MinPixelsForDebrisCreation) CreateNewDebris(region);

                // PixelLostByDivision assumes the region is already gone from the grid
                // (its weighted-center math uses the post-removal pixel count).
                _grid.RemovePixels(region);

                _body.PixelLostByDivision(region);
            }
        }

        private void CreateNewDebris(HashSet<Vector2Int> points)
        {
            var rightTopPoint = new Vector2Int(points.AsValueEnumerable().Max(p => p.x),
                points.AsValueEnumerable().Max(p => p.y));
            var leftBottomPoint = new Vector2Int(points.AsValueEnumerable().Min(p => p.x),
                points.AsValueEnumerable().Min(p => p.y));
            var parentCenterPoint = _grid.Center;

            var width = rightTopPoint.x - leftBottomPoint.x + 1;
            var height = rightTopPoint.y - leftBottomPoint.y + 1;

            var centrePoint = leftBottomPoint + new Vector2(width, height) / 2;

            var newColorsGrid = new Color32[width, height];

            foreach (var point in points)
                newColorsGrid[point.x - leftBottomPoint.x, point.y - leftBottomPoint.y] = _grid.GetValue(point);

            var globalPosition = _body.transform.TransformPoint(centrePoint - parentCenterPoint);

            _debrisSpawner.SpawnDebris(globalPosition, _body.transform.rotation, newColorsGrid, _body);
        }
    }
}