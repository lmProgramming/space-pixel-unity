using System.Collections.Generic;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class DestroyCollidingPixel : CollisionResolver
    {
        public DestroyCollidingPixel(PixelCollisionHandler collisionHandler, PixelatedRigidbody pixelatedRigidbody) :
            base(collisionHandler, pixelatedRigidbody)
        {
        }

        public override IEnumerable<Vector2Int> ResolveCollision(PixelatedRigidbody other, Collision2D collision)
        {
            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.GetContact(0).point);

            var pixelToDestroyPosition = CollisionHandler.GetClosestPixelPosition(localPoint);

            if (pixelToDestroyPosition == null) return new List<Vector2Int>();

            var pos = pixelToDestroyPosition.Value;
            PixelatedRigidbody.RemovePixelAt(pos);

            return new[] { new Vector2Int(pos.x, pos.y) };
        }
    }
}