using System;
using UnityEngine;

public interface IWeapon
{
    void Shoot();

    bool IsReady();

    GameObject GetIcon();

    event Action OnReady;
    event Action OnNotReady;
}