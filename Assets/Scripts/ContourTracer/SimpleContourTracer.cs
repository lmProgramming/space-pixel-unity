using System;
using System.Collections.Generic;
using UnityEngine;

namespace ContourTracer
{
    public class SimpleContourTracer : IContourTracer
    {
        // Each contour is stored as a stack of pixel positions (in texture space)
        private readonly List<Stack<Vector2Int>> _contours = new();
        private readonly uint _gapLength;
        private readonly float _smoothingThreshold;
        private Vector2 _pivotOffset;

        // Used to convert pixel coordinates to world coordinates
        private float _pointScale;

        public SimpleContourTracer(uint gapLength, float smoothingThreshold)
        {
            _gapLength = gapLength;
            _smoothingThreshold = smoothingThreshold;
        }

        /// <summary>
        ///     Number of detected contours.
        /// </summary>
        private int ContourCount { get; set; }

        /// <summary>
        ///     Traces the contours (i.e. borders of non-transparent pixels) in the given texture.
        /// </summary>
        /// <param name="texture">The texture to process.</param>
        /// <param name="pivot">
        ///     Normalized pivot point used to offset the final world coordinates.
        ///     (e.g. (0.5, 0.5) is the center of the texture)
        /// </param>
        /// <param name="pixelsPerUnit">Scale to convert pixels to world units.</param>
        public Vector2[] GenerateCollider(Texture2D texture, Vector2 pivot, float pixelsPerUnit)
        {
            ContourCount = 0;
            _pointScale = 1f / pixelsPerUnit;

            // Use effective dimensions to avoid multiplying by zero for small sprites.
            var effectiveWidth = texture.width > 1 ? texture.width - 1f : texture.width;
            var effectiveHeight = texture.height > 1 ? texture.height - 1f : texture.height;

            pivot.x *= effectiveWidth;
            pivot.y *= effectiveHeight;
            _pivotOffset = pivot * _pointScale;

            if (texture.width == 1 || texture.height == 1)
            {
                HandleSmallSprite();
                return null;
            }

            // State variables for the tracing algorithm
            var currentPoint = Vector2Int.zero;
            var currentDirection = Direction.Front;
            Code currentCode; // starting state

            // Variables used for handling line segments and smoothing.
            Code lastLineCode;
            float currentLineLength;
            float maxAllowedLineLength;
            Vector2 lastSegmentDirection;

            var pixels = texture.GetRawTextureData<Color32>();
            // Keep track of pixels that have already been processed.
            HashSet<Vector2Int> processedPixels = new();
            Stack<Vector2Int> currentContour;
            var insideAlreadyProcessed = false;

            #region Helper Methods (Local Functions)

            // Checks if the pixel at (x, y) is part of the border (non-transparent)
            bool IsPixelBorder(int x, int y)
            {
                var index = y * texture.width + x;
                return pixels[index].a != 0f;
            }

            // Checks if (x, y) is within texture bounds and is a border pixel.
            bool IsPixelBorderSafe(int x, int y)
            {
                return x >= 0 && x < texture.width &&
                       y >= 0 && y < texture.height &&
                       IsPixelBorder(x, y);
            }

            // Rotates an offset (dx, dy) according to the current tracing direction.
            void RotateOffset(ref int dx, ref int dy)
            {
                int temp;
                switch (currentDirection)
                {
                    case Direction.Right:
                        temp = dx;
                        dx = dy;
                        dy = -temp;
                        break;
                    case Direction.Rear:
                        dx = -dx;
                        dy = -dy;
                        break;
                    case Direction.Left:
                        temp = dx;
                        dx = -dy;
                        dy = temp;
                        break;
                    // When facing Front, no rotation is needed.
                }
            }

            // Checks a neighboring pixel given an offset (dx, dy), after applying the current rotation.
            bool CheckNeighbor(int dx, int dy)
            {
                RotateOffset(ref dx, ref dy);
                return IsPixelBorderSafe(currentPoint.x + dx, currentPoint.y + dy);
            }

            // Neighbor functions relative to the current point and direction.
            bool Neighbor0()
            {
                return IsPixelBorder(currentPoint.x, currentPoint.y);
            }

            bool Neighbor1()
            {
                return CheckNeighbor(-1, -1);
            }

            bool Neighbor2()
            {
                return CheckNeighbor(-1, 0);
            }

            bool Neighbor3()
            {
                return CheckNeighbor(-1, 1);
            }

            bool Neighbor4()
            {
                return CheckNeighbor(0, 1);
            }

            // Records the current point into the contour stack based on the movement type.
            void EncodeStep(Code newCode)
            {
                switch (newCode)
                {
                    case Code.Inner:
                        if (currentCode != Code.Outer)
                        {
                            if (currentCode == Code.Straight)
                            {
                                if (lastLineCode == Code.Inner)
                                {
                                    // Remove the redundant last point and smooth the segment.
                                    var lastPoint = currentContour.Pop();
                                    SmoothSegment();
                                    currentContour.Push(lastPoint);

                                    lastSegmentDirection = Vector2.zero;
                                    currentContour.Push(currentPoint);
                                }
                                else if (currentLineLength >= maxAllowedLineLength)
                                {
                                    currentContour.Push(currentPoint);
                                }
                            }
                            else
                            {
                                currentContour.Push(currentPoint);
                            }
                        }

                        maxAllowedLineLength = currentLineLength + _gapLength;
                        currentLineLength = 0;
                        break;

                    case Code.InnerOuter:
                        if (currentCode != Code.InnerOuter)
                            currentContour.Push(currentPoint);
                        break;

                    case Code.Straight:
                        if (currentCode != Code.Straight)
                        {
                            lastLineCode = currentCode;
                            if (currentCode == Code.Outer)
                            {
                                if (currentContour.Peek() == currentPoint)
                                    break;
                                SmoothSegment();
                            }

                            currentContour.Push(currentPoint);
                        }

                        currentLineLength++;
                        break;

                    case Code.Outer:
                        if (currentCode != Code.Inner)
                        {
                            if (currentCode == Code.Straight)
                            {
                                if (lastLineCode != Code.Inner || currentLineLength > maxAllowedLineLength)
                                {
                                    maxAllowedLineLength = float.PositiveInfinity;
                                    lastSegmentDirection = Vector2.zero;
                                }
                                else
                                {
                                    currentContour.Pop();
                                    SmoothSegment();
                                }
                            }
                            else if (currentCode == Code.Outer)
                            {
                                if (currentContour.Peek() == currentPoint)
                                    break;
                                SmoothSegment();
                                lastSegmentDirection = Vector2.zero;
                            }

                            currentContour.Push(currentPoint);
                            currentLineLength = float.PositiveInfinity;
                        }

                        break;
                }

                currentCode = newCode;
            }

            void HandleSmallSprite()
            {
                int minX = int.MaxValue, minY = int.MaxValue;
                int maxX = int.MinValue, maxY = int.MinValue;
                for (var x = 0; x < texture.width; x++)
                for (var y = 0; y < texture.height; y++)
                    if (texture.GetPixel(x, y).a > 0f)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }

                if (minX == int.MaxValue)
                    return;

                var left = minX;
                var right = maxX + 1;
                var bottom = minY;
                var top = maxY + 1;

                var contour = new Stack<Vector2Int>();
                contour.Push(new Vector2Int(left, bottom));
                contour.Push(new Vector2Int(right, bottom));
                contour.Push(new Vector2Int(right, top));
                contour.Push(new Vector2Int(left, top));

                // Add the contour to the list.
                _contours.Add(contour);
                ContourCount = 1;
            }

            // Moves the current point by an offset (dx, dy) adjusted by the current direction.
            void MovePoint(int dx, int dy)
            {
                RotateOffset(ref dx, ref dy);
                currentPoint.x += dx;
                currentPoint.y += dy;
                processedPixels.Add(currentPoint);
            }

            // Adjusts the current direction by turning (using the Direction enum values).
            void ChangeDirection(Direction turn)
            {
                // The Direction enum is defined so that adding the enum values works modulo 4.
                currentDirection = (Direction)(((int)currentDirection + (int)turn) % 4);
            }

            // Checks and smooths the contour by removing redundant points if the segment is nearly collinear.
            void SmoothSegment()
            {
                var segmentDir = (currentPoint - (Vector2)currentContour.Peek()).normalized;
                if (Vector2.Dot(segmentDir, lastSegmentDirection) > _smoothingThreshold)
                    currentContour.Pop();
                lastSegmentDirection = segmentDir;
            }

            #endregion

            // Main loop: iterate over every pixel in the texture.
            for (currentPoint.x = 0; currentPoint.x < texture.width; ++currentPoint.x)
            for (currentPoint.y = 0; currentPoint.y < texture.height; ++currentPoint.y)
            {
                // Skip pixels that have already been processed as part of a contour.
                if (processedPixels.Contains(currentPoint))
                {
                    insideAlreadyProcessed = true;
                    continue;
                }

                var isCurrentBorder = Neighbor0();
                if (insideAlreadyProcessed)
                {
                    insideAlreadyProcessed = isCurrentBorder;
                }
                else if (isCurrentBorder)
                {
                    // Start a new contour.
                    if (ContourCount >= _contours.Count)
                        _contours.Add(new Stack<Vector2Int>());
                    else
                        _contours[ContourCount].Clear();
                    currentContour = _contours[ContourCount];

                    // Save starting state so we know when the contour is closed.
                    var startingPoint = currentPoint;
                    var startingDirection = currentDirection;

                    // Initialize state variables.
                    currentCode = Code.InnerOuter;
                    lastLineCode = Code.Straight;
                    currentLineLength = 0;
                    maxAllowedLineLength = float.PositiveInfinity;
                    lastSegmentDirection = Vector2.zero;

                    // Trace around the border until we return to the start.
                    do
                    {
                        // Stage 1: Process left-side neighbors.
                        if (Neighbor1())
                        {
                            if (Neighbor2())
                            {
                                // Case 1: Inner corner.
                                EncodeStep(Code.Inner);
                                MovePoint(-1, -1);
                                ChangeDirection(Direction.Rear);
                            }
                            else
                            {
                                // Case 2: Transition between inner and outer.
                                EncodeStep(Code.InnerOuter);
                                MovePoint(-1, -1);
                                ChangeDirection(Direction.Rear);
                            }
                        }
                        else
                        {
                            if (Neighbor2())
                            {
                                // Case 3: Straight edge.
                                EncodeStep(Code.Straight);
                                MovePoint(-1, 0);
                                ChangeDirection(Direction.Left);
                            }
                            else
                            {
                                // Case 4: Outer edge.
                                EncodeStep(Code.Outer);
                            }
                        }

                        // Stage 2: Process right-side neighbors.
                        if (Neighbor3())
                        {
                            if (Neighbor4())
                            {
                                // Case 6: Inner corner on the right.
                                EncodeStep(Code.Inner);
                                MovePoint(-1, 1);
                            }
                            else
                            {
                                // Case 5: Transition between inner and outer.
                                EncodeStep(Code.InnerOuter);
                                MovePoint(-1, 1);
                            }
                        }
                        else if (Neighbor4())
                        {
                            // Case 7: Straight edge on the right.
                            EncodeStep(Code.Straight);
                            MovePoint(0, 1);
                            ChangeDirection(Direction.Right);
                        }
                        else
                        {
                            // Case 8: Outer edge on the right.
                            EncodeStep(Code.Outer);
                            ChangeDirection(Direction.Rear);
                        }
                    } while (currentPoint != startingPoint || currentDirection != startingDirection);

                    // Final adjustments after completing the loop.
                    if (currentCode == Code.Straight && lastLineCode == Code.Inner)
                        currentContour.Pop();

                    SmoothSegment();

                    // Only count valid contours that have at least three points.
                    if (currentContour.Count >= 3)
                        ContourCount++;

                    insideAlreadyProcessed = true;
                }
            }

            return null;
        }

        /// <summary>
        ///     Returns the contour at the given index as an array of world-space points.
        /// </summary>
        public Vector2[] GetContour(int index)
        {
            var pixelPoints = _contours[index].ToArray();
            return Array.ConvertAll(pixelPoints, pixelPoint => (Vector2)pixelPoint * _pointScale - _pivotOffset);
        }

        // Enum representing the current facing direction.
        private enum Direction
        {
            Front,
            Right,
            Rear,
            Left
        }

        // Enum representing the type of movement or transition during contour tracing.
        private enum Code
        {
            Inner,
            InnerOuter,
            Straight,
            Outer
        }
    }
}