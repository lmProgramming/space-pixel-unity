using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public abstract class CollisionResolver
    {
        protected readonly PixelCollisionHandler CollisionHandler;
        protected readonly IPixelatedRigidbody PixelatedRigidbody;

        protected CollisionResolver(PixelCollisionHandler collisionHandler, IPixelatedRigidbody pixelatedRigidbody)
        {
            CollisionHandler = collisionHandler;
            PixelatedRigidbody = pixelatedRigidbody;
        }

        public abstract IEnumerable<Vector2Int> ResolveCollision(IPixelatedRigidbody other, Collision2D collision);
    }
}