using System;
using System.Collections.Generic;
using Core.Grid;
using Core.Ship;
using LMPro.External.IsAlive;
using UnityEngine;

namespace Core.Pixelation
{
    public interface IPixelatedRigidbody : IPixelated, IHasAliveCheck
    {
        float MassMultiplier { get; }
        bool HasSprite { get; }
        Vector2 WeightedCenter { get; }
        Vector2 WorldWeightedCenter { get; }
        ITexturePixelGrid TexturePixelGrid { get; set; }
        IPixelCollisionHandler CollisionHandler { get; }
        Rigidbody2D Rigidbody { get; }
        SpriteRenderer SpriteRenderer { get; set; }
        Transform Transform { get; }
        GameObject GameObject { get; }
        float DefaultPixelHealthForSnapshot { get; }
        float MaxArmorHealthForSnapshot { get; }
        Vector2 WorldToLocalPoint(Vector2 worldPosition);
        Vector2Int WorldToLocalPixel(Vector2 worldPosition);
        Vector2 LocalToWorldPoint(Vector2Int localPosition);
        Vector2 LocalToWorldPoint(Vector2 localPosition);
        ArmorGridSnapshot CaptureArmorGridSnapshot();
        HealthGridSnapshot CaptureHealthGridSnapshot();
        void ApplyArmorGridSnapshot(ArmorGridSnapshot snapshot);
        void ApplyHealthGridSnapshot(HealthGridSnapshot snapshot);
        void NoPixelsLeft();
        event Action<IPixelated> OnNoPixelsLeft;
        event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;
    }
}