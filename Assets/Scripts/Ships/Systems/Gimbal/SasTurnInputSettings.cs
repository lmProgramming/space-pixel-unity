using System;
using UnityEngine;

namespace Ships.Systems.Gimbal
{
    [Serializable]
    public class SasTurnInputSettings
    {
        [field: SerializeField] public float TurnReleaseThreshold { get; private set; } = 0.05f;
        [field: SerializeField] public float MinTurnInputChange { get; private set; } = 0.01f;
        [field: SerializeField] public float MinDesiredDirectionSquareMagnitude { get; private set; } = 0.0001f;
        [field: SerializeField] public float HeadingGain { get; private set; } = 0.04f;
        [field: SerializeField] public float AngularVelocityGain { get; private set; } = 0.03f;
        [field: SerializeField] public float MaxTurnInput { get; private set; } = 2f;
        [field: SerializeField] public float ForwardCompensationStrength { get; private set; } = 1f;
        [field: SerializeField] public float ForwardCompensationMaxTurnInput { get; private set; } = 1.5f;
    }
}