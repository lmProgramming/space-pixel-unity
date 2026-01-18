using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public interface IPixelatedRigidbody : IPixelated
    {
        float MassMultiplier { get; }
        bool HasSprite { get; }
        PixelGrid PixelGrid { get; set; }
        IPixelCollisionHandler CollisionHandler { get; }
        Rigidbody2D Rigidbody { get; }
        SpriteRenderer SpriteRenderer { get; set; }
        Transform Transform { get; }
        GameObject GameObject { get; }
        Vector2 WorldToLocalPoint(Vector2 worldPosition);
        Vector2Int WorldToLocalPixel(Vector2 worldPosition);
        Vector2 LocalToWorldPoint(Vector2Int localPosition);
        event Action<IPixelated> OnNoPixelsLeft;
        event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;
    }
}