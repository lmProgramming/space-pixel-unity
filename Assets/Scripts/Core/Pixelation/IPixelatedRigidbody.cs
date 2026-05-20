using System;
using System.Collections.Generic;
using Core.Grid;
using UnityEngine;

namespace Core.Pixelation
{
    public interface IPixelatedRigidbody : IPixelated
    {
        float MassMultiplier { get; }
        bool HasSprite { get; }
        Vector2 WeightedCenter { get; }
        ITexturePixelGrid TexturePixelGrid { get; set; }
        IPixelCollisionHandler CollisionHandler { get; }
        Rigidbody2D Rigidbody { get; }
        SpriteRenderer SpriteRenderer { get; set; }
        Transform Transform { get; }
        GameObject GameObject { get; }
        Vector2 WorldToLocalPoint(Vector2 worldPosition);
        Vector2Int WorldToLocalPixel(Vector2 worldPosition);
        Vector2 LocalToWorldPoint(Vector2Int localPosition);
        Vector2 LocalToWorldPoint(Vector2 localPosition);
        event Action<IPixelated> OnNoPixelsLeft;
        event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;
    }
}