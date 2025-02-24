using System.Collections.Generic;
using UnityEngine;

namespace Pixelation
{
    public interface IPixelated
    {
        public void RemovePixelAt(Vector2Int point);

        public void RemovePixels(HashSet<Vector2Int> points);

        public void SetSpriteFromColors(Color[,] colors);

        public void SetPixelNoApply(Vector2Int point, Color color);

        public void SetPixel(Vector2Int point, Color color);

        public void ApplyChanges();

        public Color GetColor(Vector2Int point);

        public bool IsPixel(Vector2Int point);

        public bool IsPixelAssumeInBounds(Vector2Int point);

        public bool InBounds(Vector2Int point);
    }
}