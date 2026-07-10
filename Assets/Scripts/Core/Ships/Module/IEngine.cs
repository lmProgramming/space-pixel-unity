using UnityEngine;

namespace Core.Ships.Module
{
    public interface IEngine : IModule
    {
        float MaxThrust { get; }
        Vector2 WorldThrustPoint { get; }
        Vector2 WorldThrustDirection { get; }
        void SetActive(bool active);
        void SetCurrentThrust(float currentThrust);
        void RotateThrusterTowards(float targetAngle, float deltaTime);
    }
}