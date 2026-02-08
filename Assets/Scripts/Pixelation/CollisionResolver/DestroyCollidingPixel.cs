using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class DestroyCollidingPixel : CollisionResolver
    {
        public DestroyCollidingPixel(PixelCollisionHandler collisionHandler, IPixelatedRigidbody pixelatedRigidbody) :
            base(collisionHandler, pixelatedRigidbody)
        {
        }

        public override IEnumerable<Vector2Int> ResolveCollision(IPixelatedRigidbody other, Collision2D collision)
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