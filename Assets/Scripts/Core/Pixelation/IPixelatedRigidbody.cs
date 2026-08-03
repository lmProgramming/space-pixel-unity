using System;
using System.Collections.Generic;
using Core.Grid;
using Core.Ships.Snapshots.PixelatedRigidbody;
using Core.Snapshot;
using LMPro.External.IsAlive;
using UnityEngine;

namespace Core.Pixelation
{
    public interface IPixelatedRigidbody : IPixelated, IHasAliveCheck, ISnapshottable<PixelatedRigidbodySnapshot>
    {
        float MassMultiplier { get; }
        bool HasSprite { get; }
        Vector2 WeightedCenter { get; }
        Vector2 WorldWeightedCenter { get; }
        ITexturePixelGrid TexturePixelGrid { get; }
        IPixelCollisionHandler CollisionHandler { get; }
        Rigidbody2D Rigidbody { get; }
        SpriteRenderer SpriteRenderer { get; set; }
        Collider2D Collider2D { get; }
        Transform Transform { get; }
        GameObject GameObject { get; }
        float DefaultPixelHealthForSnapshot { get; }
        float MaxArmorHealthForSnapshot { get; }
        Vector2 WorldToLocalPoint(Vector2 worldPosition);
        Vector2Int WorldToLocalPixel(Vector2 worldPosition);
        Vector2 LocalToWorldPoint(Vector2Int localPosition);
        Vector2 LocalToWorldPoint(Vector2 localPosition);
        void NoPixelsLeft();
        event Action<IPixelatedRigidbody> Destroyed;
        event Action<List<Vector2Int>, PixelLoseReason> OnPixelsLost;
        event Action<List<Vector2Int>> OnPixelsRestored;
        Color32[,] BuildPristineColors();
        float[,] BuildPristineHealth(Color32[,] pristineColors);
    }
}