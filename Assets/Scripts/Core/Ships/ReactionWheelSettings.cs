using System;
using UnityEngine;

namespace Core.Ships
{
    [Serializable]
    public class ReactionWheelSettings
    {
        public float dampingStrength = 5.0f;
        public float maxTorque = 1.0f;

        public float angularVelocityDeadZoneDegreesPerSecond =
            Mathf.Epsilon;

        public float angularVelocityAtWhichSetItToZero =
            0.0001f;
    }
}