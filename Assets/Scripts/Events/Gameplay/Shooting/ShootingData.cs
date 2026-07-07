using System;
using Core.Ships;
using UnityEngine;

namespace Events.Gameplay.Shooting
{
    [Serializable]
    public abstract class ShootingData
    {
        public Vector2 point;
        public Vector2 direction;
        public IShip InstigatorShip;

        public ShootingData(IShip instigatorShip, Vector2 point, Vector2 direction)
        {
            InstigatorShip = instigatorShip;
            this.point = point;
            this.direction = direction;
        }
    }
}