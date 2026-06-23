using Core.Ship;
using UnityEngine;

namespace Events.Gameplay.Shooting
{
    public class BulletShootingData : ShootingData
    {
        public readonly float Momentum;

        public BulletShootingData(IShip instigatorShip, Vector2 point, Vector2 direction, float momentum) : base(
            instigatorShip, point, direction)
        {
            Momentum = momentum;
        }
    }
}