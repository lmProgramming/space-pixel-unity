using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pixelation
{
    public class PixelGrid : IPixelated
    {
        private readonly SpriteRenderer _spriteRenderer;
        private Sprite _internalSprite;
        private Texture2D _internalTexture;

        public PixelGrid(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
        }

        public int Width => Texture.width;
        public int Height => Texture.height;

        public Vector2 Center => new((float)Width / 2, (float)Height / 2);

        public Texture2D Texture { get; private set; }

        public void SetSpriteFromColors(Color[,] colors)
        {
            var colorsArray = new Color[colors.GetLength(0) * colors.GetLength(1)];

            for (var y = 0; y < colors.GetLength(1); y++)
            for (var x = 0; x < colors.GetLength(0); x++)
                colorsArray[y * colors.GetLength(0) + x] = colors[x, y];

            SetSpriteFromColors(colorsArray, colors.GetLength(0), colors.GetLength(1));
        }

        public void SetPixelNoApply(Vector2Int point, Color color)
        {
            Texture.SetPixel(point.x, point.y, color);
        }

        public void SetPixel(Vector2Int point, Color color)
        {
            Texture.SetPixel(point.x, point.y, color);
            Texture.Apply();
        }

        public void ApplyChanges()
        {
            Texture.Apply();
        }

        public Color GetColor(Vector2Int point)
        {
            return Texture.GetPixel(point.x, point.y);
        }

        public bool IsPixel(Vector2Int point)
        {
            return InBounds(point) && IsPixelAssumeInBounds(point);
        }

        public bool IsPixelAssumeInBounds(Vector2Int point)
        {
            return Texture.GetPixel(point.x, point.y).a > 0;
        }

        public bool InBounds(Vector2Int point)
        {
            return point.x >= 0 && point.x < Texture.width && point.y >= 0 && point.y < Texture.height;
        }

        public void RemovePixelAt(Vector2Int point)
        {
            SetPixel(point, Color.clear);
        }

        public void RemovePixels(HashSet<Vector2Int> points)
        {
            foreach (var point in points) SetPixel(point, Color.clear);
        }

        private void SetSpriteFromColors(Color[] colors, int width, int height)
        {
            Texture = new Texture2D(width, height, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point
            };

            Texture.SetPixels(colors);
            Texture.Apply();

            _internalSprite = Sprite.Create(Texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), 1);
        }

        public void SetSprite(Sprite sprite)
        {
            SetSpriteFromColors(sprite.texture.GetPixels(), sprite.texture.width, sprite.texture.height);
        }

        public void Setup()
        {
            _spriteRenderer.sprite = _internalSprite;
        }

        public Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast)
        {
            var pointsTraversed = GridMarcher.March(new Vector2Int(Texture.width, Texture.height), startPosition,
                direction);

            if (getLast) pointsTraversed.Reverse();

            foreach (var point in pointsTraversed.Where(IsPixel))
                return new Vector2Int(point.x, point.y);

            return null;
        }
    }
}