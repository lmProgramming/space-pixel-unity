using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;

namespace Ships.Systems.Gimbal
{
    public class SasTurnInputResolver
    {
        private float _desiredHeadingDegrees;
        private bool _hasDesiredHeading;
        private bool _wasTurning;

        public void CaptureDesiredHeading(float currentHeadingDegrees)
        {
            _desiredHeadingDegrees = currentHeadingDegrees;
            _hasDesiredHeading = true;
        }

        public float ResolveTurnInput(float requestedTurnInput, float forwardInput, float horizontalInput,
            Rigidbody2D selfRigidbody,
            float currentHeadingDegrees, IReadOnlyList<Engine> engines, Vector2 shipForward, Vector2 centerOfMass,
            float maxLeverArm, SasTurnInputSettings settings)
        {
            UpdateDesiredHeadingOnTurnRelease(requestedTurnInput, currentHeadingDegrees, settings);

            var headingHoldTurnInput = GetHeadingHoldTurnInput(requestedTurnInput, selfRigidbody, currentHeadingDegrees,
                settings);
            if (Mathf.Abs(requestedTurnInput) > settings.TurnReleaseThreshold ||
                IsMovementInputIdle(forwardInput, horizontalInput, settings))
                return headingHoldTurnInput;

            var forwardCompensation = CalculateThrustCompensationTurnInput(forwardInput, horizontalInput, engines,
                shipForward,
                centerOfMass, maxLeverArm, settings);
            var withForwardCompensation = headingHoldTurnInput +
                                          forwardCompensation * settings.ForwardCompensationStrength;

            return Mathf.Clamp(withForwardCompensation, -settings.MaxTurnInput, settings.MaxTurnInput);
        }

        private void UpdateDesiredHeadingOnTurnRelease(float requestedTurnInput, float currentHeadingDegrees,
            SasTurnInputSettings settings)
        {
            CaptureCurrentHeadingIfNeeded(currentHeadingDegrees);

            var isTurning = Mathf.Abs(requestedTurnInput) > settings.TurnReleaseThreshold;
            if (_wasTurning && !isTurning)
                CaptureDesiredHeading(currentHeadingDegrees);

            _wasTurning = isTurning;
        }

        private float GetHeadingHoldTurnInput(float requestedTurnInput, Rigidbody2D selfRigidbody,
            float currentHeadingDegrees, SasTurnInputSettings settings)
        {
            if (Mathf.Abs(requestedTurnInput) > settings.TurnReleaseThreshold)
                return requestedTurnInput;

            CaptureCurrentHeadingIfNeeded(currentHeadingDegrees);

            var headingError = GetPredictiveHeadingError(currentHeadingDegrees, _desiredHeadingDegrees,
                selfRigidbody.angularVelocity, settings.PredictionHorizon);

            if (Mathf.Abs(headingError) < settings.HeadingDeadZoneDegrees &&
                Mathf.Abs(selfRigidbody.angularVelocity) < settings.AngularVelocityDeadZoneDegreesPerSecond)
            {
                CaptureDesiredHeading(currentHeadingDegrees);
                return 0f;
            }

            var angularVelocityDamping = -selfRigidbody.angularVelocity * settings.AngularVelocityGain;
            var turnCorrection = headingError * settings.HeadingGain + angularVelocityDamping;

            if (Mathf.Abs(turnCorrection) < settings.MinTurnInputChange)
                turnCorrection = 0f;

            return Mathf.Clamp(turnCorrection, -settings.MaxTurnInput, settings.MaxTurnInput);
        }

        private static bool IsMovementInputIdle(float forwardInput, float horizontalInput,
            SasTurnInputSettings settings)
        {
            return Mathf.Abs(forwardInput) <= settings.MovementInputDeadZone &&
                   Mathf.Abs(horizontalInput) <= settings.MovementInputDeadZone;
        }

        private static float GetPredictiveHeadingError(float currentHeadingDegrees, float desiredHeadingDegrees,
            float angularVelocityDegreesPerSecond, float predictionHorizonSeconds)
        {
            var predictedHeading = currentHeadingDegrees + angularVelocityDegreesPerSecond * predictionHorizonSeconds;
            return Mathf.DeltaAngle(predictedHeading, desiredHeadingDegrees);
        }

        private static float CalculateThrustCompensationTurnInput(float forwardInput, float horizontalInput,
            IReadOnlyList<Engine> engines,
            Vector2 shipForward, Vector2 centerOfMass, float maxLeverArm, SasTurnInputSettings settings)
        {
            if (engines.Count == 0)
                return 0f;

            const float sampleTurnInput = 0.2f;

            var baselineTorque = EngineDirectionSolver.EstimateNetTorqueForTurnInput(engines, shipForward,
                centerOfMass, maxLeverArm, forwardInput, horizontalInput, 0f);
            if (Mathf.Abs(baselineTorque) <= 0.0001f)
                return 0f;

            var positiveSampleTorque = EngineDirectionSolver.EstimateNetTorqueForTurnInput(engines, shipForward,
                centerOfMass, maxLeverArm, forwardInput, horizontalInput, sampleTurnInput);
            var negativeSampleTorque = EngineDirectionSolver.EstimateNetTorqueForTurnInput(engines, shipForward,
                centerOfMass, maxLeverArm, forwardInput, horizontalInput, -sampleTurnInput);

            var torqueSlope = (positiveSampleTorque - negativeSampleTorque) / (sampleTurnInput * 2f);
            if (Mathf.Abs(torqueSlope) <= 0.0001f)
                return 0f;

            var compensation = -baselineTorque / torqueSlope;
            return Mathf.Clamp(compensation, -settings.ForwardCompensationMaxTurnInput,
                settings.ForwardCompensationMaxTurnInput);
        }

        private void CaptureCurrentHeadingIfNeeded(float currentHeadingDegrees)
        {
            if (!_hasDesiredHeading)
                CaptureDesiredHeading(currentHeadingDegrees);
        }
    }
}