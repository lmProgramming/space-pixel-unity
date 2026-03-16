using System.Collections.Generic;
using UnityEngine;

namespace LMPro
{
    public static class MathExt
    {
        public static float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
        {
            return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
        }

        public static float SimpleDistance(Vector2 a, Vector2 b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public static T RandomFrom<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("The list is empty or null. Returning default value.");
#endif
                return default;
            }

            var randomIndex = Random.Range(0, list.Count);
            return list[randomIndex];
        }

        public static T RandomFrom<T>(List<T> list, T defaultReturn)
        {
            if (list == null || list.Count == 0) return defaultReturn;

            var randomIndex = Random.Range(0, list.Count);
            return list[randomIndex];
        }

        public static T RandomFrom<T>(T[] array)
        {
            if (array == null || array.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("The list is empty or null. Returning default value.");
#endif
                return default;
            }

            var randomIndex = Random.Range(0, array.Length);
            return array[randomIndex];
        }

        /// <summary>
        ///     Randomly selects an element from the specified list, removes that element from the list, and returns it.
        /// </summary>
        /// <returns>
        ///     The element that was removed from the list, or <c>default(T)</c> if the list is <c>null</c> or contains no
        ///     elements.
        /// </returns>
        public static T RandomPullFrom<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("The list is empty or null. Returning default value.");
                return default;
            }

            var randomIndex = Random.Range(0, list.Count);

            var value = list[randomIndex];

            list.RemoveAt(randomIndex);
            return value;
        }

        public static List<Vector2Int> FindGridIntersections(Vector2 aLineStart, Vector2 aDir, float aDistance)
        {
            var aResult = new List<Vector2Int>();

            aDistance *= aDistance;
            // vertical grid lines
            var x = aLineStart.x;
            if (aDir.x > 0.0001f)
            {
                var v = new Vector2(Mathf.Ceil(x) - aLineStart.x, 0f);
                v.y = v.x / aDir.x * aDir.y;
                while (v.sqrMagnitude < aDistance)
                {
                    aResult.Add(new Vector2Int((int)v.x, (int)v.y));

                    v.x += 1;
                    v.y = v.x / aDir.x * aDir.y;
                }
            }
            else if (aDir.x < -0.0001f)
            {
                var v = new Vector2(Mathf.Floor(x) - aLineStart.x, 0f);
                v.y = v.x / aDir.x * aDir.y;
                while (v.sqrMagnitude < aDistance)
                {
                    aResult.Add(new Vector2Int((int)v.x, (int)v.y));

                    v.x -= 1;
                    v.y = v.x / aDir.x * aDir.y;
                }
            }

            // horizontal grid lines
            var y = aLineStart.y;
            if (aDir.y > 0.0001f)
            {
                var v = new Vector2(0f, Mathf.Ceil(y) - aLineStart.y);
                v.x = v.y / aDir.y * aDir.x;
                while (v.sqrMagnitude < aDistance)
                {
                    aResult.Add(new Vector2Int((int)v.x, (int)v.y));

                    v.y += 1;
                    v.x = v.y / aDir.y * aDir.x;
                }
            }
            else if (aDir.y < -0.0001f)
            {
                var v = new Vector2(0f, Mathf.Floor(y) - aLineStart.y);
                v.x = v.y / aDir.y * aDir.x;
                while (v.sqrMagnitude < aDistance)
                {
                    aResult.Add(new Vector2Int((int)v.x, (int)v.y));

                    v.y -= 1;
                    v.x = v.y / aDir.y * aDir.x;
                }
            }

            aResult.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));

            return aResult;
        }

        public static float Angle360(float angle)
        {
            if (angle < 0) return angle + 360;

            return angle;
        }

        // angle between -90 and 90 symmetrical
        public static float AngleSym180(float angle)
        {
            switch (angle)
            {
                case > 90:
                    angle -= (angle - 90) * 2;
                    break;
                case < -90:
                    angle += (angle + 90) * 2;
                    break;
            }

            return angle;
        }

        public static bool FacingRight(float angle)
        {
            return angle is <= 90 or >= 270;
        }

        public static Vector2 GetXYDirection(float angle, float magnitude)
        {
            var rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            var xYZDirection = rotation * new Vector3(magnitude, 0f, 0f);
            return xYZDirection;
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        public static Vector2[] GetTriangleApexes(float sideLength)
        {
            var halfSideLength = sideLength / 2;
            var triangleHeight = halfSideLength * Mathf.Sqrt(3);

            var triangleCenter = new Vector2(0, triangleHeight / 2);
            var triangleBias = new Vector2(0, triangleHeight / 3) - triangleCenter;

            var vertexA = triangleBias + new Vector2(0, 2 * triangleHeight / 3);
            var vertexB = triangleBias + new Vector2(-sideLength / 2, -triangleHeight / 3);
            var vertexC = triangleBias + new Vector2(sideLength / 2, -triangleHeight / 3);

            Vector2[] vertices = { vertexA, vertexB, vertexC };

            return vertices;
        }

        public static int RandomInclusive(int x, int y)
        {
            return Random.Range(x, y + 1);
        }
    }
}