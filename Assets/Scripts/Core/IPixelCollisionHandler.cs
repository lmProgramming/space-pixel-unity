using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Core
{
    public interface IPixelCollisionHandler
    {
        void RaiseCollisionEvent([CanBeNull] IPixelatedRigidbody other, Vector2 contactPoint, Vector2Int[] pixels);
        Vector2Int? GetPointAlongPath(Vector2Int startPosition, Vector2 direction, bool getLast);
        List<Vector2Int> GetClosestPixelPositions(Vector2 localPosition, int positionsMaxCount);
        Vector2Int? GetClosestPixelPosition(Vector2 localPosition);
        void SetCollided(bool isCollided);
        void OnCollision(Collision2D collision);
        void ResolveCollision(IPixelatedRigidbody other, Collision2D collision);
    }
}