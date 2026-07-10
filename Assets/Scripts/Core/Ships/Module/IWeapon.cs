using System;
using UnityEngine;

namespace Core.Ships.Module
{
    public interface IWeapon : IModule
    {
        void Shoot();
        void StopShooting();

        bool IsReady();

        Sprite GetSprite();

        event Action OnReady;
        event Action OnNotReady;
    }
}