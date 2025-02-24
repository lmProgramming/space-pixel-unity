using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class DestroyCollidingPixel : CollisionResolver
    {
        public DestroyCollidingPixel(PixelCollisionHandler collisionHandler, PixelatedRigidbody pixelatedRigidbody) :
            base(collisionHandler, pixelatedRigidbody)
        {
        }

        public override void ResolveCollision(IPixelated other, Collision2D collision)
        {
            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.contacts[0].point);

            // var pixelToDestroyPosition = GetPointAlongPath(hitPosition, -collision.rigidbody.linearVelocity, true) ??
            //                              GetPointAlongPath(hitPosition, collision.rigidbody.linearVelocity, false);

            var pixelToDestroyPosition = CollisionHandler.GetClosestPixelPosition(localPoint);

            if (pixelToDestroyPosition == null) return;

            var pos = pixelToDestroyPosition.Value;
            PixelatedRigidbody.RemovePixelAt(pos);
        }
    }
}