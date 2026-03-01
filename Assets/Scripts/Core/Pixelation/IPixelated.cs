using System.Collections.Generic;
using UnityEngine;

namespace Core.Pixelation
{
    public interface IPixelated
    {
        int CurrentPixelCount { get; }
        int StartPixelCount { get; }

        public void RemovePixelAt(Vector2Int point, bool simulateCollision = false);

        public void RemovePixels(IEnumerable<Vector2Int> points, bool simulateCollision = false);

        public bool DamagePixelAt(Vector2Int point, float damage, bool simulateCollision = false);

        public List<Vector2Int> DamagePixels(IEnumerable<Vector2Int> points, float damagePerPixel,
            bool simulateCollision = false);

        public void SetTextureFromColors(Color32[,] colors);

        public void SetPixelNoApply(Vector2Int point, Color32 color);

        public void SetPixel(Vector2Int point, Color32 color);

        public void ApplyPixels();

        public Color32 GetColor(Vector2Int point);

        public bool IsPixel(Vector2Int point);

        public bool IsPixelAssumeInBounds(Vector2Int point);

        public bool InBounds(Vector2Int point);

        public Vector2Int Dimensions();
    }
}