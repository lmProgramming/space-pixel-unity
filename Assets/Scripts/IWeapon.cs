using System;

public interface IWeapon
{
    void Shoot();

    bool IsReady();

    event Action OnReady;
    event Action OnNotReady;
}