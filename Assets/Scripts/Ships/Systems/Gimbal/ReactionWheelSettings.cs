using System;
using UnityEngine;

namespace Ships.Systems.Gimbal
{
    [Serializable]
    public class ReactionWheelSettings
    {
        [field: SerializeField] public float DampingStrength { get; private set; } = 5.0f;
        [field: SerializeField] public float MaxTorque { get; private set; } = 1.0f;

        [field: SerializeField] public float AngularVelocityDeadZoneDegreesPerSecond { get; private set; } =
            Mathf.Epsilon;

        [field: SerializeField] public float AngularVelocityAtWhichSetItToZero { get; private set; } =
            0.0001f;
    }
}