using System.Collections.Generic;
using UnityEngine;

namespace Pixelation
{
    public interface IPixelated
    {
        public void RemovePixelAt(Vector2Int point);

        public void RemovePixels(IEnumerable<Vector2Int> points);

        public void SetSpriteFromColors(Color32[,] colors);

        public void SetPixelNoApply(Vector2Int point, Color32 color);

        public void SetPixel(Vector2Int point, Color32 color);

        public void ApplyPixels();

        public Color32 GetColor(Vector2Int point);

        public bool IsPixel(Vector2Int point);

        public bool IsPixelAssumeInBounds(Vector2Int point);

        public bool InBounds(Vector2Int point);
    }
}