using UnityEngine;

namespace ContourTracer
{
    public interface IContourTracer
    {
        public Vector2[] GenerateCollider(Texture2D sourceTexture, Vector2 centerPivot, float pixelsPerUnit);
    }
}