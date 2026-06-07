using System.Collections.Generic;
using Core.Constants;
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
            var totalDamage = collision.relativeVelocity.magnitude * Mathf.Sqrt(other.Rigidbody.mass) *
                              GameplayConstants.PixelDamageMultiplier;

            var localPoint = PixelatedRigidbody.WorldToLocalPoint(collision.GetContact(0).point);

            var pixelCount = Mathf.Max(1, (int)totalDamage);
            var pixelsToDamage = CollisionHandler.GetClosestPixelPositions(localPoint, pixelCount);

            if (pixelsToDamage.Count == 0) return pixelsToDamage;

            var damagePerPixel = totalDamage / pixelsToDamage.Count;

            return PixelatedRigidbody.DamagePixels(pixelsToDamage, damagePerPixel);
        }
    }
}