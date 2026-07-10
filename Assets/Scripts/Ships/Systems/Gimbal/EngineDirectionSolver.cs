using System.Collections.Generic;
using Core.Ships.Module;
using UnityEngine;
using ZLinq;

namespace Ships.Systems.Gimbal
{
    public class EngineDirectionSolver
    {
        public static float GetMaxLeverArmLength(IEnumerable<IEngine> engines, Vector2 centerOfMass)
        {
            var maxLeverArm = engines.AsValueEnumerable().Select(engine => engine.WorldThrustPoint - centerOfMass)
                .Aggregate(0f, (current, lever) => Mathf.Max(current, lever.magnitude));

            return Mathf.Max(maxLeverArm, 0.01f);
        }

        public static Vector2 GetShipRight(Vector2 shipForward)
        {
            return new Vector2(shipForward.y, -shipForward.x);
        }

        public static Vector2 GetDesiredEngineDirection(Vector2 shipForward, Vector2 centerOfMass, float maxLeverArm,
            IEngine engine, float forwardInput, float horizontalInput, float turnInput)
        {
            var lever = engine.WorldThrustPoint - centerOfMass;
            var rotationalDirection = new Vector2(-lever.y, lever.x) / maxLeverArm;
            return shipForward * forwardInput + GetShipRight(shipForward) * horizontalInput +
                   rotationalDirection * turnInput;
        }

        public static float EstimateNetTorqueForTurnInput(IReadOnlyList<IEngine> engines, Vector2 shipForward,
            Vector2 centerOfMass, float maxLeverArm, float forwardInput, float horizontalInput, float turnInput)
        {
            return (from engine in engines.AsValueEnumerable()
                where !(engine.MaxThrust <= 0f)
                let desiredDirection =
                    GetDesiredEngineDirection(shipForward, centerOfMass, maxLeverArm, engine, forwardInput,
                        horizontalInput, turnInput)
                where !(desiredDirection.sqrMagnitude <= Mathf.Epsilon)
                let thrust = Mathf.Clamp01(desiredDirection.magnitude) * engine.MaxThrust
                let force = desiredDirection.normalized * thrust
                let lever = engine.WorldThrustPoint - centerOfMass
                select lever.x * force.y - lever.y * force.x).Sum();
        }
    }
}