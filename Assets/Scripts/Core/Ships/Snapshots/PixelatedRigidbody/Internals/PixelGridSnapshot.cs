using System;
using System.Collections.Generic;
using Core.Grid;
using Unity.Collections;
using UnityEngine;
using ZLinq;

namespace Core.Ships.Snapshots.PixelatedRigidbody.Internals
{
    [Serializable]
    public class PixelGridSnapshot : ITexturePixelGrid
    {
        /// <summary>
        ///     Flattened array of pixel colors. Empty pixels are transparent (alpha = 0).
        ///     Row-major order: index = y * width + x
        /// </summary>
        [SerializeField] private Color32[] pixels;

        [SerializeField] private int width;
        [SerializeField] private int height;

        public PixelGridSnapshot()
        {
        }

        public PixelGridSnapshot(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Color32[width * height];
        }

        public Vector2 Center => new(Width / 2f, Height / 2f);

        public int Width
        {
            get => width;
            private set => width = value;
        }

        public int Height
        {
            get => height;
            private set => height = value;
        }

        public Color32 GetValue(Vector2Int point)
        {
            return pixels[point.y * Width + point.x];
        }

        public Color32[,] GetValues2D()
        {
            var result = new Color32[Width, Height];

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                result[x, y] = pixels[y * Width + x];

            return result;
        }

        public bool InBounds(Vector2Int point)
        {
            return point.x >= 0 &&
                   point.x < Width &&
                   point.y >= 0 &&
                   point.y < Height;
        }

        public Vector2Int Dimensions()
        {
            return new Vector2Int(Width, Height);
        }

        public void SetTextureFromColors(NativeArray<Color32> colors, int newWidth, int newHeight)
        {
            Width = newWidth;
            Height = newHeight;
            pixels = colors.ToArray();
        }

        public void SetTextureFromColors(Color32[] colors, int newWidth, int newHeight)
        {
            Width = newWidth;
            Height = newHeight;
            pixels = new Color32[colors.Length];
            Array.Copy(colors, pixels, colors.Length);
        }

        public void SetTextureFromColors(Color32[,] colors)
        {
            Width = colors.GetLength(0);
            Height = colors.GetLength(1);

            pixels = new Color32[Width * Height];

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                pixels[y * Width + x] = colors[x, y];
        }

        public void SetPixel(Vector2Int point, Color32 color)
        {
            if (point.x < 0 || point.x >= Width || point.y < 0 || point.y >= Height)
                return;
            if (pixels == null || pixels.Length != Width * Height)
                return;
            pixels[point.y * Width + point.x] = color;
        }

        public bool IsPixel(Vector2Int point)
        {
            if (point.x < 0 || point.x >= Width || point.y < 0 || point.y >= Height)
                return false;
            if (pixels == null || pixels.Length != Width * Height)
                return false;
            return pixels[point.y * Width + point.x].a > 0;
        }

        public bool IsPixelAssumeInBounds(Vector2Int point)
        {
            return pixels[point.y * Width + point.x].a > 0;
        }

        public void RemovePixelAt(Vector2Int point)
        {
            RemovePixel(point.x, point.y);
        }

        public void RemovePixels(IEnumerable<Vector2Int> points)
        {
            foreach (var point in points)
                RemovePixelAt(point);
        }

        public Vector2Int? GetFirstPixelAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
        {
            throw new NotSupportedException(
                $"{nameof(GetFirstPixelAlongPath)} is not supported by {nameof(PixelGridSnapshot)}.");
        }

        public Texture2D Texture => null;

        public int PixelCount
        {
            get { return pixels.AsValueEnumerable().Count(pixel => pixel.a > 0); }
        }

        public void SetTexture(Texture2D texture)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            SetTextureFromColors(texture.GetPixels32(), texture.width, texture.height);
        }

        public void Setup()
        {
            // No setup required for snapshots.
        }

        public void SetPixelNoApply(Vector2Int point, Color32 color)
        {
            SetPixel(point, color);
        }

        public void ApplyPixels()
        {
            // Snapshot has no Texture2D to apply changes to.
        }

        public void SetPixel(int x, int y, Color32 color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
            if (pixels == null || pixels.Length != Width * Height)
                return;
            pixels[y * Width + x] = color;
        }

        public void RemovePixel(int x, int y)
        {
            SetPixel(x, y, new Color32(0, 0, 0, 0));
        }

        public List<Vector2Int> GetAllNonTransparentPixelPositions()
        {
            var positions = new List<Vector2Int>();
            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
                if (IsPixel(new Vector2Int(x, y)))
                    positions.Add(new Vector2Int(x, y));

            return positions;
        }
    }
}