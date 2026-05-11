using UnityEngine;

namespace Core.Grid
{
    public interface ITexturePixelGrid : ISimplePixelGrid
    {
        Texture2D Texture { get; }
        int PixelCount { get; }
        void SetTexture(Texture2D texture);
        void Setup();
        void SetPixelNoApply(Vector2Int point, Color32 color);
        void ApplyPixels();
    }
}