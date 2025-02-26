using System.Collections.Generic;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class PhysicsCollision : CollisionResolver
    {
        public PhysicsCollision(PixelCollisionHandler collisionHandler, PixelatedRigidbody pixelatedRigidbody) : base(
            collisionHandler, pixelatedRigidbody)
        {
        }

        public override IEnumerable<Vector2Int> ResolveCollision(PixelatedRigidbody other, Collision2D collision)
        {
            var pixelsToDestroyCount = collision.relativeVelocity.magnitude * Mathf.Sqrt(other.Rigidbody.mass) * 0.01f;

            Debug.Log(pixelsToDestroyCount);

            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.contacts[0].point);

            var pixelsToDestroy = CollisionHandler.GetClosestPixelPositions(localPoint, (int)pixelsToDestroyCount);

            PixelatedRigidbody.RemovePixels(pixelsToDestroy);

            return pixelsToDestroy;
        }
    }
}