using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Core.Grid
{
    public interface IPixelGrid
    {
        Vector2 Center { get; }
        int Width { get; }
        int Height { get; }
        Texture2D Texture { get; }
        void SetTextureFromColors(NativeArray<Color32> colors, int width, int height);
        void SetTextureFromColors(Color32[] colors, int width, int height);
        void SetTexture(Texture2D texture);
        void Setup();
        Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast);
        void SetPixelNoApply(Vector2Int point, Color32 color);
        void SetPixel(Vector2Int point, Color32 color);
        void ApplyPixels();
        Color32 GetColor(Vector2Int point);
        bool IsPixel(Vector2Int point);
        bool IsPixelAssumeInBounds(Vector2Int point);
        bool InBounds(Vector2Int point);
        Vector2Int Dimensions();
        void RemovePixelAt(Vector2Int point, bool _);
        void RemovePixels(IEnumerable<Vector2Int> points, bool _);
        void SetTextureFromColors(Color32[,] colors);
    }
}