using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Core.Grid
{
    public interface IPixelGrid : IGrid<Color32>
    {
        Texture2D Texture { get; }
        int PixelCount { get; }
        void SetTextureFromColors(NativeArray<Color32> colors, int width, int height);
        void SetTextureFromColors(Color32[] colors, int width, int height);
        void SetTexture(Texture2D texture);
        void Setup();
        void SetPixelNoApply(Vector2Int point, Color32 color);
        void SetPixel(Vector2Int point, Color32 color);
        void ApplyPixels();
        bool IsPixel(Vector2Int point);
        bool IsPixelAssumeInBounds(Vector2Int point);
        void RemovePixelAt(Vector2Int point);
        void RemovePixels(IEnumerable<Vector2Int> points);
        void SetTextureFromColors(Color32[,] colors);
        Vector2Int? GetFirstPixelAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast);
    }
}