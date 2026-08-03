using UnityEngine;

namespace Core.Pixelation
{
    public readonly struct Pixel
    {
        public Pixel(Vector2Int point, Color32 color, float health)
        {
            Point = point;
            Color = color;
            Health = health;
        }

        public Vector2Int Point { get; }
        public Color32 Color { get; }
        public float Health { get; }
    }
}