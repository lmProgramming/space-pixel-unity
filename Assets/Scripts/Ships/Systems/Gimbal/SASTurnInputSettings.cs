using System;
using UnityEngine;

namespace Ships.Systems.Gimbal
{
    [Serializable]
    public class SASTurnInputSettings
    {
        [field: SerializeField] public float TurnReleaseThreshold { get; private set; } = 0.05f;
        [field: SerializeField] public float MovementInputDeadZone { get; private set; } = 0.05f;
        [field: SerializeField] public float MinTurnInputChange { get; private set; } = 0.01f;
        [field: SerializeField] public float HeadingDeadZoneDegrees { get; private set; } = 0.3f;
        [field: SerializeField] public float AngularVelocityDeadZoneDegreesPerSecond { get; private set; } = 0.1f;
        [field: SerializeField] public float MinAppliedThrustRatio { get; private set; } = 0.01f;
        [field: SerializeField] public float MinDesiredDirectionSquareMagnitude { get; private set; } = 0.0001f;
        [field: SerializeField] public float HeadingGain { get; private set; } = 0.04f;
        [field: SerializeField] public float AngularVelocityGain { get; private set; } = 0.03f;

        [field: SerializeField] [field: Range(0f, 0.6f)]
        public float PredictionHorizon { get; private set; } = 0.3f;

        [field: SerializeField] public float MaxTurnInput { get; private set; } = 2f;
        [field: SerializeField] public float ForwardCompensationStrength { get; private set; } = 1f;
        [field: SerializeField] public float ForwardCompensationMaxTurnInput { get; private set; } = 1.5f;
    }
}