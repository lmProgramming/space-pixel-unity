using Core.Pixelation;
using UnityEngine;

namespace Core.Ship
{
    public enum ModuleType
    {
        Command,
        Production,
        Storage,
        Weapon,
        Engine
    }

    public interface IModule
    {
        ModuleType Type { get; }
        IPixelatedRigidbody PixelatedRigidbody { get; }
        Transform Transform { get; }
    }
}