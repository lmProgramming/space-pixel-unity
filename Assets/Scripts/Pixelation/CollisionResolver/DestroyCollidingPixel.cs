using System.Collections.Generic;
using Core.Constants;
using Core.Pixelation;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public class DestroyCollidingPixel : CollisionResolver
    {
        public DestroyCollidingPixel(PixelCollisionHandler collisionHandler, IPixelatedRigidbody pixelatedRigidbody,
            GameplayConstants gameplayConstants) :
            base(collisionHandler, pixelatedRigidbody, gameplayConstants)
        {
        }

        public override IEnumerable<Vector2Int> ResolveCollision(IPixelatedRigidbody other, Collision2D collision)
        {
            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.GetContact(0).point);

            var pixelToDestroyPosition = CollisionHandler.GetClosestPixelPosition(localPoint);

            if (pixelToDestroyPosition == null) return new List<Vector2Int>();

            var pos = pixelToDestroyPosition.Value;
            var destroyed = PixelatedRigidbody.DamagePixelAt(pos, 1f);

            return destroyed ? new[] { pos } : new List<Vector2Int>();
        }
    }
}