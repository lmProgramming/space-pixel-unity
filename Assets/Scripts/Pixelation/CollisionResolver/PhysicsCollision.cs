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

        public override IEnumerable<Vector2Int> ResolveCollision(IPixelated other, Collision2D collision)
        {
            var pixelsToDestroyCount = collision.relativeVelocity.magnitude * collision.rigidbody.mass / 500;

            Debug.Log(pixelsToDestroyCount);

            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.contacts[0].point);

            var pixelsToDestroy = CollisionHandler.GetClosestPixelPositions(localPoint, (int)pixelsToDestroyCount);

            PixelatedRigidbody.RemovePixels(pixelsToDestroy);

            return pixelsToDestroy;
        }
    }
}