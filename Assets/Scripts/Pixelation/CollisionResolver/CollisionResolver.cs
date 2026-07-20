using System.Collections.Generic;
using Core.Constants;
using Core.Pixelation;
using UnityEngine;

namespace Pixelation.CollisionResolver
{
    public abstract class CollisionResolver
    {
        protected readonly PixelCollisionHandler CollisionHandler;
        protected readonly IPixelatedRigidbody PixelatedRigidbody;
        protected readonly float PixelDamageMultiplier;

        protected CollisionResolver(PixelCollisionHandler collisionHandler, IPixelatedRigidbody pixelatedRigidbody,
            GameplayConstants gameplayConstants)
        {
            CollisionHandler = collisionHandler;
            PixelatedRigidbody = pixelatedRigidbody;
            PixelDamageMultiplier = gameplayConstants.pixelDamageMultiplier;
        }

        public abstract IEnumerable<Vector2Int> ResolveCollision(IPixelatedRigidbody other, Collision2D collision);
    }
}