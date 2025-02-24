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

        public abstract void ResolveCollision(IPixelated other, Collision2D collision);
    }
}