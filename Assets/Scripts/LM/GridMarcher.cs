using System.Collections.Generic;
using UnityEngine;

namespace LM
{
    public static class GridMarcher
    {
        public static List<Vector2Int> March(Vector2Int dimensions, Vector2Int start, Vector2 direction)
        {
            direction.Normalize();

            var m = direction.y / direction.x;

            var x = start.x;
            var y = (float)start.y;

            var triesLeft = 3;
            while (triesLeft > 0 && !(x < dimensions.x && x >= 0 && y < dimensions.y && y >= 0))
            {
                triesLeft--;

                x += direction.x > 0 ? 1 : -1;
                y += m;
            }

            var points = new List<Vector2Int>();

            while (x < dimensions.x && x >= 0 && y < dimensions.y && y >= 0)
            {
                points.Add(new Vector2Int(x, (int)y));

                x += direction.x > 0 ? 1 : -1;
                y += m;
            }

            return points;
        }
    }
}