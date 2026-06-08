using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;
using ZLinq;

namespace Ships.Systems.Gimbal
{
    public class EngineDirectionSolver
    {
        public float GetMaxLeverArmLength(IEnumerable<Engine> engines, Vector2 centerOfMass)
        {
            var maxLeverArm = engines.AsValueEnumerable().Select(engine => engine.WorldThrustPoint - centerOfMass)
                .Aggregate(0f, (current, lever) => Mathf.Max(current, lever.magnitude));

            return Mathf.Max(maxLeverArm, 0.01f);
        }

        public static Vector2 GetDesiredEngineDirection(Vector2 shipForward, Vector2 centerOfMass, float maxLeverArm,
            Engine engine, float forwardInput, float turnInput)
        {
            var lever = engine.WorldThrustPoint - centerOfMass;
            var rotationalDirection = new Vector2(-lever.y, lever.x) / maxLeverArm;
            return shipForward * forwardInput + rotationalDirection * turnInput;
        }

        public float EstimateNetTorqueForTurnInput(IReadOnlyList<Engine> engines, Vector2 shipForward,
            Vector2 centerOfMass, float maxLeverArm, float forwardInput, float turnInput)
        {
            return (from engine in engines.AsValueEnumerable()
                where !(engine.MaxThrust <= 0f)
                let desiredDirection =
                    GetDesiredEngineDirection(shipForward, centerOfMass, maxLeverArm, engine, forwardInput, turnInput)
                where !(desiredDirection.sqrMagnitude <= Mathf.Epsilon)
                let thrust = Mathf.Clamp01(desiredDirection.magnitude) * engine.MaxThrust
                let force = desiredDirection.normalized * thrust
                let lever = engine.WorldThrustPoint - centerOfMass
                select lever.x * force.y - lever.y * force.x).Sum();
        }
    }
}