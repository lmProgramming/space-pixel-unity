using System.Collections.Generic;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public abstract class CollisionResolver
    {
        protected readonly PixelCollisionHandler CollisionHandler;
        protected readonly PixelatedRigidbody PixelatedRigidbody;

        protected CollisionResolver(PixelCollisionHandler collisionHandler, PixelatedRigidbody pixelatedRigidbody)
        {
            CollisionHandler = collisionHandler;
            PixelatedRigidbody = pixelatedRigidbody;
        }

        public abstract IEnumerable<Vector2Int> ResolveCollision(PixelatedRigidbody other, Collision2D collision);
    }
}