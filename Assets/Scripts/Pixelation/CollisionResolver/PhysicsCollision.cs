using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class PhysicsCollision : CollisionResolver
    {
        public PhysicsCollision(PixelCollisionHandler collisionHandler, IPixelatedRigidbody pixelatedRigidbody) : base(
            collisionHandler, pixelatedRigidbody)
        {
        }

        public override IEnumerable<Vector2Int> ResolveCollision(IPixelatedRigidbody other, Collision2D collision)
        {
            var pixelsToDestroyCount = collision.relativeVelocity.magnitude * Mathf.Sqrt(other.Rigidbody.mass) * 0.01f;

            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.GetContact(0).point);

            var pixelsToDestroy = CollisionHandler.GetClosestPixelPositions(localPoint, (int)pixelsToDestroyCount);

            PixelatedRigidbody.RemovePixels(pixelsToDestroy);

            return pixelsToDestroy;
        }
    }
}