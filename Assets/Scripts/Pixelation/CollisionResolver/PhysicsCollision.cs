using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class PhysicsCollision : CollisionResolver
    {
        public PhysicsCollision(PixelCollisionHandler collisionHandler, PixelatedRigidbody pixelatedRigidbody) : base(
            collisionHandler, pixelatedRigidbody)
        {
        }

        public override void ResolveCollision(IPixelated other, Collision2D collision)
        {
            var pixelsToDestroy = collision.relativeVelocity.magnitude * collision.rigidbody.mass / 500;

            Debug.Log(pixelsToDestroy);

            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.contacts[0].point);

            var pixelToDestroyPosition = CollisionHandler.GetClosestPixelPositions(localPoint, (int)pixelsToDestroy);

            PixelatedRigidbody.RemovePixels(pixelToDestroyPosition);
        }
    }
}