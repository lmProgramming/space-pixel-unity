using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Core.Grid
{
    public interface ISimplePixelGrid : IGrid<Color32>
    {
        void SetTextureFromColors(NativeArray<Color32> colors, int newWidth, int newHeight);
        void SetTextureFromColors(Color32[] colors, int newWidth, int newHeight);
        void SetTextureFromColors(Color32[,] colors);
        void SetPixel(Vector2Int point, Color32 color);
        bool IsPixel(Vector2Int point);
        bool IsPixelAssumeInBounds(Vector2Int point);
        void RemovePixelAt(Vector2Int point);
        void RemovePixels(IEnumerable<Vector2Int> points);
        void AddPixelAt(Vector2Int point, Color32 color);
        void AddPixels(IEnumerable<(Vector2Int point, Color32 color)> pixels);
        Vector2Int? GetFirstPixelAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast);
    }
}