using System.Collections.Generic;
using Pixelation;
using UnityEngine;

namespace LM
{
    public static class OverlapCalculator
    {
        public static List<Vector2Int> CalculateOverlappingPoints(PixelatedRigidbody body1, PixelatedRigidbody body2)
        {
            var body1Dimensions = body1.Dimensions();
            var body2Dimensions = body2.Dimensions();

            var body1LeftBottom = body1.LocalToWorldPoint(new Vector2Int(0, 0));
            var body1RightTop = body1.LocalToWorldPoint(new Vector2Int(body1Dimensions.x - 1, body1Dimensions.y - 1));

            var body2LeftBottom = body2.LocalToWorldPoint(new Vector2Int(0, 0)) - Vector2Int.one;
            var body2RightTop = body2.LocalToWorldPoint(new Vector2Int(body2Dimensions.x, body2Dimensions.y));

            var overlapLeftBottom = new Vector2Int(
                Mathf.Max(Mathf.RoundToInt(body1LeftBottom.x), Mathf.RoundToInt(body2LeftBottom.x)),
                Mathf.Max(Mathf.RoundToInt(body1LeftBottom.y), Mathf.RoundToInt(body2LeftBottom.y))
            );

            var overlapRightTop = new Vector2Int(
                Mathf.Min(Mathf.RoundToInt(body1RightTop.x), Mathf.RoundToInt(body2RightTop.x)),
                Mathf.Min(Mathf.RoundToInt(body1RightTop.y), Mathf.RoundToInt(body2RightTop.y))
            );

            var overlappingPoints = new List<Vector2Int>();

            for (var x = overlapLeftBottom.x; x <= overlapRightTop.x; x++)
            for (var y = overlapLeftBottom.y; y <= overlapRightTop.y; y++)
            {
                var point = new Vector2Int(x, y);
                var point1 = body1.WorldToLocalPixel(point);
                var point2 = body2.WorldToLocalPixel(point);
                if (body1.IsPixel(point1) && (body2.IsPixel(point2 + new Vector2Int(1, 0)) ||
                                              body2.IsPixel(point2 + new Vector2Int(0, 1)) ||
                                              body2.IsPixel(point2 + new Vector2Int(-1, 0)) ||
                                              body2.IsPixel(point2 + new Vector2Int(0, -1))))
                    overlappingPoints.Add(point1);
            }

            return overlappingPoints;
        }
    }
}