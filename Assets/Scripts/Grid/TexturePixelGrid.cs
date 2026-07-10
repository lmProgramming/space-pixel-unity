using System;
using System.Collections.Generic;
using Core.Grid;
using LMPro;
using LMPro.DataStructures;
using Unity.Collections;
using UnityEngine;
using ZLinq;

namespace Grid
{
    public class TexturePixelGrid : ITexturePixelGrid
    {
        private readonly SpriteRenderer _spriteRenderer;
        private Sprite _internalSprite;
        private Texture2D _internalTexture;

        public TexturePixelGrid(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
        }

        public void SetPixelNoApply(Vector2Int point, Color32 color)
        {
            Texture.SetPixel(point.x, point.y, color);
        }

        public void SetPixel(Vector2Int point, Color32 color)
        {
            Texture.SetPixel(point.x, point.y, color);
            Texture.Apply();
        }

        public void ApplyPixels()
        {
            Texture.Apply();
        }

        public bool IsPixel(Vector2Int point)
        {
            return InBounds(point) && IsPixelAssumeInBounds(point);
        }

        public bool IsPixelAssumeInBounds(Vector2Int point)
        {
            return Texture.GetPixel(point.x, point.y).a > 0;
        }

        public Color32 GetValue(Vector2Int point)
        {
            return Texture.GetPixel(point.x, point.y);
        }

        public Color32[,] GetValues2D()
        {
            return Helpers.Make2DArray(Texture.GetPixels32(), Texture.height, Texture.width);
        }

        public bool InBounds(Vector2Int point)
        {
            return point.x >= 0 && point.x < Texture.width && point.y >= 0 && point.y < Texture.height;
        }

        public Vector2Int Dimensions()
        {
            return new Vector2Int(Texture.width, Texture.height);
        }

        public void SetTextureFromColors(Color32[,] colors)
        {
            var width = colors.GetLength(0);
            var height = colors.GetLength(1);
            var colorsArray = new Color32[width * height];

            if (width == 0 || height == 0)
            {
                Debug.LogWarning("Pixel grid texture has no colors.");
                return;
            }

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                colorsArray[y * width + x] = colors[x, y];

            //var lengthOfColor32 = Marshal.SizeOf(typeof(Color32));
            //Buffer.BlockCopy(colors, 0, colorsArray, 0, colorsArray.Length * lengthOfColor32);

            SetTextureFromColors(colorsArray, colors.GetLength(0), colors.GetLength(1));
        }

        public int Width => Texture.width;
        public int Height => Texture.height;
        public int PixelCount { get; private set; }

        public Vector2 Center => new((float)Width / 2, (float)Height / 2);

        public Texture2D Texture { get; private set; }

        public void SetTextureFromColors(NativeArray<Color32> colors, int width, int height)
        {
            Texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };

            Texture.SetPixelData(colors, 0);
            Texture.Apply();

            _internalSprite = Sprite.Create(Texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 1);

            PixelCount = colors.AsValueEnumerable().Count(c => c.a > 0);
        }

        public void SetTextureFromColors(Color32[] colors, int width, int height)
        {
            Texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };

            Texture.SetPixels32(colors);
            Texture.Apply();

            _internalSprite = Sprite.Create(Texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 1);

            PixelCount = colors.AsValueEnumerable().Count(c => c.a > 0);
        }

        public void SetTexture(Texture2D texture)
        {
            SetTextureFromColors(texture.GetPixels32(), texture.width, texture.height);
        }

        public void Setup()
        {
            _spriteRenderer.sprite = _internalSprite;
        }

        public Vector2Int? GetFirstPixelAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
        {
            var pointsTraversed = GridMarcher.March(new Vector2Int(Texture.width, Texture.height), startPosition,
                direction);

            if (getLast) pointsTraversed.Reverse();

            foreach (var point in pointsTraversed.AsValueEnumerable().Where(IsPixel))
                return new Vector2Int(point.x, point.y);

            return null;
        }

        public void RemovePixelAt(Vector2Int point)
        {
            if (PixelCount <= 0)
                throw new InvalidOperationException("[TexturePixelGrid] Pixel count would go below zero.");

            SetPixel(point, Color.clear);
            PixelCount--;
        }

        public void RemovePixels(IEnumerable<Vector2Int> points)
        {
            var pointsList = points.AsValueEnumerable().ToList();

            if (PixelCount < pointsList.Count)
                throw new InvalidOperationException("[TexturePixelGrid] Pixel count would go below zero.");

            foreach (var point in pointsList) SetPixelNoApply(point, Color.clear);
            ApplyPixels();
            PixelCount -= pointsList.Count;
        }
    }
}