using System;
using Core.Gameplay.Combat;
using UnityEngine;

namespace Ships.Modules
{
    public abstract class WeaponBase : Module, IWeapon
    {
        public abstract void Shoot();
        public abstract void StopShooting();
        public abstract bool IsReady();
        public abstract Sprite GetSprite();

        public event Action OnReady;
        public event Action OnNotReady;


        protected virtual void HandleReady()
        {
            OnReady?.Invoke();
        }

        protected virtual void HandleNotReady()
        {
            OnNotReady?.Invoke();
        }
    }
}