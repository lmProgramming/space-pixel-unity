using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;
using ZLinq;

namespace Ships.Internal
{
    public class ControlAllocator
    {
        public static float[] AllocateControlInputs(IReadOnlyList<Engine> engines,
            IReadOnlyList<Vector2> desiredDirections,
            Vector2 centerOfMass, Vector2 forward, float forwardInput, float turnInput, float maxLeverArm,
            ControlAllocatorSettings settings)
        {
            var thrustRatios = new float[engines.Count];
            var requestedForceDirection = forwardInput >= 0f ? forward : -forward;
            var requestedForceMagnitude = Mathf.Abs(forwardInput);

            var columns = new Vector3[engines.Count];
            var hasAnyEffectiveColumn = false;
            var totalThrustCapacity = 0f;
            var totalTorqueCapacity = 0f;

            for (var i = 0; i < engines.Count; i++)
            {
                var engine = engines[i];
                if (engine.MaxThrust <= 0f) continue;

                if (desiredDirections[i].sqrMagnitude <= Mathf.Epsilon) continue;

                var dir = desiredDirections[i].normalized;
                var worldDotDesired = Vector2.Dot(engine.WorldThrustDirection, dir);
                if (worldDotDesired <= 0f) continue;

                var lever = engine.WorldThrustPoint - centerOfMass;
                var torquePerUnit = lever.x * dir.y - lever.y * dir.x;

                columns[i] = new Vector3(
                    dir.x * engine.MaxThrust * settings.ForceWeight,
                    dir.y * engine.MaxThrust * settings.ForceWeight,
                    torquePerUnit * engine.MaxThrust * settings.TorqueWeight);

                hasAnyEffectiveColumn = hasAnyEffectiveColumn || columns[i].sqrMagnitude > Mathf.Epsilon;

                totalThrustCapacity += engine.MaxThrust;
                totalTorqueCapacity += Mathf.Abs(torquePerUnit) * engine.MaxThrust;
            }

            if (!hasAnyEffectiveColumn) return thrustRatios;

            var targetForceMagnitude = requestedForceMagnitude * totalThrustCapacity * settings.ForceWeight;
            var targetX = requestedForceDirection.x * targetForceMagnitude;
            var targetY = requestedForceDirection.y * targetForceMagnitude;

            var torqueScale = Mathf.Max(totalTorqueCapacity, Mathf.Max(1f, maxLeverArm));
            var targetTorque = turnInput * torqueScale * settings.TorqueWeight;

            var forceNormalization = Mathf.Max(totalThrustCapacity * settings.ForceWeight, 1f);
            var torqueNormalization = Mathf.Max(torqueScale * settings.TorqueWeight, 1f);

            for (var i = 0; i < columns.Length; i++)
            {
                if (columns[i].sqrMagnitude <= Mathf.Epsilon) continue;

                columns[i] = new Vector3(
                    columns[i].x / forceNormalization,
                    columns[i].y / forceNormalization,
                    columns[i].z / torqueNormalization);
            }

            targetX /= forceNormalization;
            targetY /= forceNormalization;
            targetTorque /= torqueNormalization;

            var denominator = Mathf.Max(0.0001f, settings.Regularization) +
                              columns.AsValueEnumerable().Sum(t => t.sqrMagnitude);

            var stepSize = 1f / denominator;
            var iterations = Mathf.Clamp(settings.Iterations, 1, 64);

            for (var iteration = 0; iteration < iterations; iteration++)
            {
                var residualX = -targetX;
                var residualY = -targetY;
                var residualTorque = -targetTorque;

                for (var i = 0; i < thrustRatios.Length; i++)
                {
                    residualX += columns[i].x * thrustRatios[i];
                    residualY += columns[i].y * thrustRatios[i];
                    residualTorque += columns[i].z * thrustRatios[i];
                }

                for (var i = 0; i < thrustRatios.Length; i++)
                {
                    if (columns[i].sqrMagnitude <= Mathf.Epsilon) continue;

                    var gradient = columns[i].x * residualX + columns[i].y * residualY +
                                   columns[i].z * residualTorque + settings.Regularization * thrustRatios[i];

                    thrustRatios[i] = Mathf.Clamp01(thrustRatios[i] - stepSize * gradient);
                }
            }

            return thrustRatios;
        }
    }
}