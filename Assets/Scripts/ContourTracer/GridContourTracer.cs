using System.Collections.Generic;
using UnityEngine;

namespace ContourTracer
{
    public class GridContourTracer : IContourTracer
    {
        /// <summary>
        ///     Generates a collider path from the source texture.
        ///     The texture is assumed to have no holes (every filled pixel touches another filled pixel except at the boundary),
        ///     and any pixel with an alpha > 0 is considered filled.
        ///     The resulting polygon runs along pixel edges.
        ///     centerPivot: normalized pivot (e.g. (0.5, 0.5) for center) used to offset the polygon.
        ///     pixelsPerUnit: scale factor to convert pixel coordinates into world units.
        /// </summary>
        public Vector2[] GenerateCollider(Texture2D sourceTexture, Vector2 centerPivot, float pixelsPerUnit)
        {
            // Step 1: Convert texture to a boolean grid.
            var texWidth = sourceTexture.width;
            var texHeight = sourceTexture.height;
            var grid = new bool[texWidth][];
            for (var index = 0; index < texWidth; index++) grid[index] = new bool[texHeight];

            // Note: GetPixel() assumes (0,0) is bottom left.
            for (var x = 0; x < texWidth; x++)
            for (var y = 0; y < texHeight; y++)
            {
                var pixel = sourceTexture.GetPixel(x, y);
                grid[x][y] = pixel.a > 0f;
            }

            // Step 2: Extract boundary edges.
            var edges = new List<Edge>();

            // Vertical edges: x from 0 to texWidth, y from 0 to texHeight-1.
            for (var x = 0; x <= texWidth; x++)
            for (var y = 0; y < texHeight; y++)
            {
                // Determine filled state for cell to the left and right of this vertical line.
                var leftFilled = x - 1 >= 0 && grid[x - 1][y];
                var rightFilled = x < texWidth && grid[x][y];

                // If one side is filled and the other is not, we have an edge.
                if (leftFilled != rightFilled)
                    // The edge runs vertically from (x, y) to (x, y+1).
                    edges.Add(new Edge(new Vector2(x, y), new Vector2(x, y + 1)));
            }

            // Horizontal edges: y from 0 to texHeight, x from 0 to texWidth-1.
            for (var y = 0; y <= texHeight; y++)
            for (var x = 0; x < texWidth; x++)
            {
                var downFilled = y - 1 >= 0 && grid[x][y - 1];
                var upFilled = y < texHeight && grid[x][y];

                if (downFilled != upFilled)
                    // The edge runs horizontally from (x, y) to (x+1, y).
                    edges.Add(new Edge(new Vector2(x, y), new Vector2(x + 1, y)));
            }

            // Step 3: Chain the edges into a continuous polygon.
            var polygon = ChainEdges(edges);
            if (polygon == null || polygon.Count == 0) return null;

            // Remove duplicate last vertex if it equals the first.
            if (polygon.Count > 1 && ApproximatelyEqual(polygon[0], polygon[^1]))
                polygon.RemoveAt(polygon.Count - 1);

            // Simplify the polygon by removing collinear points.
            polygon = SimplifyPolygon(polygon);

            // (Optional) Reverse the polygon if needed so that the winding order is as expected.
            // Unity often expects the vertices to be in clockwise order.
            if (!IsClockwise(polygon))
                polygon.Reverse();

            // Step 4: Adjust for pivot and scaling.
            // The polygon is currently in pixel coordinates with the origin at (0,0).
            // First, subtract the pivot offset (in pixels) to shift the polygon.
            // centerPivot is assumed to be normalized (0-1) so that, for example, (0.5, 0.5) centers it.
            var pivotOffset = new Vector2(texWidth * centerPivot.x, texHeight * centerPivot.y);
            for (var i = 0; i < polygon.Count; i++)
                // Subtract the pivot offset and then scale to world units.
                polygon[i] = (polygon[i] - pivotOffset) / pixelsPerUnit;

            return polygon.ToArray();
        }

        /// <summary>
        ///     Chains a list of unorganized edges (each with two endpoints) into an ordered list of vertices.
        ///     Assumes that the edges form a single closed loop.
        /// </summary>
        private List<Vector2> ChainEdges(List<Edge> edges)
        {
            if (edges == null || edges.Count == 0)
                return null;

            var chain = new List<Vector2>();

            // Start with the first edge.
            var currentEdge = edges[0];
            edges.RemoveAt(0);
            var startVertex = currentEdge.P1;
            var currentVertex = currentEdge.P2;

            chain.Add(startVertex);
            chain.Add(currentVertex);

            // Keep connecting edges until we return to the start.
            while (edges.Count > 0)
            {
                var foundNext = false;
                // Look for an edge that connects to the current vertex.
                for (var i = 0; i < edges.Count; i++)
                {
                    var candidate = edges[i];
                    if (ApproximatelyEqual(candidate.P1, currentVertex))
                    {
                        currentVertex = candidate.P2;
                        chain.Add(currentVertex);
                        edges.RemoveAt(i);
                        foundNext = true;
                        break;
                    }

                    if (ApproximatelyEqual(candidate.P2, currentVertex))
                    {
                        currentVertex = candidate.P1;
                        chain.Add(currentVertex);
                        edges.RemoveAt(i);
                        foundNext = true;
                        break;
                    }
                }

                if (!foundNext)
                {
#if UNITY_EDITOR
                    Debug.LogWarning(
                        "Could not find a connecting edge. The edges might not form a single closed loop.");
#endif
                    break;
                }

                // If we’ve looped back to the start, finish.
                if (ApproximatelyEqual(currentVertex, startVertex))
                    break;
            }

            return chain;
        }

        /// <summary>
        ///     Simplifies the polygon by removing vertices that are collinear with their neighbors.
        /// </summary>
        private List<Vector2> SimplifyPolygon(List<Vector2> polygon, float tolerance = 0.001f)
        {
            if (polygon.Count < 3)
                return polygon;

            var simplified = new List<Vector2>();
            var count = polygon.Count;
            for (var i = 0; i < count; i++)
            {
                // Wrap around indices for a closed polygon.
                var prev = polygon[(i - 1 + count) % count];
                var current = polygon[i];
                var next = polygon[(i + 1) % count];

                // Compute the cross product of (current - prev) and (next - current).
                var cross = (current.x - prev.x) * (next.y - current.y) - (current.y - prev.y) * (next.x - current.x);
                if (Mathf.Abs(cross) > tolerance) simplified.Add(current);
                // Else: The point is collinear and can be skipped.
            }

            return simplified;
        }

        /// <summary>
        ///     Helper: Determines if two Vector2 values are nearly equal.
        ///     (Since we work with integer coordinates as floats, direct comparison is usually safe,
        ///     but this adds a margin.)
        /// </summary>
        private bool ApproximatelyEqual(Vector2 a, Vector2 b, float epsilon = 0.001f)
        {
            return Vector2.SqrMagnitude(a - b) < epsilon * epsilon;
        }

        /// <summary>
        ///     Returns true if the polygon vertices are in clockwise order.
        /// </summary>
        private bool IsClockwise(List<Vector2> polygon)
        {
            var sum = 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                var current = polygon[i];
                var next = polygon[(i + 1) % polygon.Count];
                sum += (next.x - current.x) * (next.y + current.y);
            }

            return sum > 0f;
        }

        // A simple structure to hold an edge defined by two points.
        private struct Edge
        {
            public readonly Vector2 P1;
            public readonly Vector2 P2;

            public Edge(Vector2 a, Vector2 b)
            {
                P1 = a;
                P2 = b;
            }
        }
    }
}