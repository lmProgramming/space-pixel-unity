using System;
using UnityEngine;

public interface IWeapon
{
    void Shoot();
    void StopShooting();

    bool IsReady();

    GameObject GetIcon();

    event Action OnReady;
    event Action OnNotReady;
}