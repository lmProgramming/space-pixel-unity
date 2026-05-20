using System;
using UnityEngine;

namespace Core.Gameplay.Combat
{
    public interface IWeapon
    {
        void Shoot();
        void StopShooting();

        bool IsReady();

        Sprite GetSprite();

        event Action OnReady;
        event Action OnNotReady;
    }
}