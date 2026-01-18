using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Pixelation
{
    public static class OverlapCalculator
    {
        private static readonly Vector2Int[] NeighborOffsets =
        {
            new(1, 0),
            new(0, 1),
            new(-1, 0),
            new(0, -1)
        };

        private static Bounds CalculateWorldBounds(IPixelatedRigidbody body)
        {
            var dimensions = body.Dimensions();
            if (dimensions.x <= 0 || dimensions.y <= 0) return new Bounds(body.Transform.position, Vector3.zero);

            var world00 = body.LocalToWorldPoint(new Vector2Int(0, 0));
            var worldW0 = body.LocalToWorldPoint(new Vector2Int(dimensions.x - 1, 0));
            var world0H = body.LocalToWorldPoint(new Vector2Int(0, dimensions.y - 1));
            var worldWh = body.LocalToWorldPoint(new Vector2Int(dimensions.x - 1, dimensions.y - 1));

            var minX = Mathf.Min(world00.x, worldW0.x, world0H.x, worldWh.x);
            var minY = Mathf.Min(world00.y, worldW0.y, world0H.y, worldWh.y);
            var maxX = Mathf.Max(world00.x, worldW0.x, world0H.x, worldWh.x);
            var maxY = Mathf.Max(world00.y, worldW0.y, world0H.y, worldWh.y);

            const float pixelWorldSizeApprox = 1.0f;
            minX -= pixelWorldSizeApprox * 0.5f + 1;
            minY -= pixelWorldSizeApprox * 0.5f + 1;
            maxX += pixelWorldSizeApprox * 0.5f + 1;
            maxY += pixelWorldSizeApprox * 0.5f + 1;

            var center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, body.Transform.position.z);
            var size = new Vector3(maxX - minX, maxY - minY, 0.1f);

            return new Bounds(center, size);
        }

        public static List<Vector2Int> CalculateOverlappingPoints(IPixelatedRigidbody body1, IPixelatedRigidbody body2)
        {
            var overlappingPoints = new List<Vector2Int>();

            if (body1 == null || body2 == null) return overlappingPoints;

            var bounds1 = CalculateWorldBounds(body1);
            var bounds2 = CalculateWorldBounds(body2);

            if (!bounds1.Intersects(bounds2)) return overlappingPoints;

            var body1Dimensions = body1.Dimensions();

            for (var x = 0; x < body1Dimensions.x; x++)
            for (var y = 0; y < body1Dimensions.y; y++)
            {
                var localP1 = new Vector2Int(x, y);

                if (!body1.IsPixelAssumeInBounds(localP1)) continue;

                var worldPosP1 = body1.LocalToWorldPoint(localP1);

                if (!bounds2.Contains(worldPosP1)) continue;

                if (IsAdjacentToBody2(worldPosP1)) overlappingPoints.Add(localP1);
            }

            return overlappingPoints;

            bool IsAdjacentToBody2(Vector2 p1WorldPos)
            {
                foreach (var offset in NeighborOffsets)
                {
                    var worldNeighborPos = p1WorldPos + offset;
                    var p2NeighborLocal = body2.WorldToLocalPixel(worldNeighborPos);
                    if (body2.IsPixel(p2NeighborLocal)) return true;
                }

                return false;
            }
        }
    }
}