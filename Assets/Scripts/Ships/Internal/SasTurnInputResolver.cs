using System.Collections.Generic;
using Ships.Modules;
using UnityEngine;
using Zenject;

namespace Ships.Internal
{
    public class SasTurnInputResolver
    {
        private readonly EngineDirectionSolver _engineDirectionSolver;

        private float _desiredHeadingDegrees;
        private bool _hasDesiredHeading;
        private bool _wasTurning;

        [Inject]
        public SasTurnInputResolver(EngineDirectionSolver engineDirectionSolver)
        {
            _engineDirectionSolver = engineDirectionSolver;
        }

        public void CaptureDesiredHeading(float currentHeadingDegrees)
        {
            _desiredHeadingDegrees = currentHeadingDegrees;
            _hasDesiredHeading = true;
        }

        public float ResolveTurnInput(float requestedTurnInput, float forwardInput, Rigidbody2D selfRigidbody,
            float currentHeadingDegrees, IReadOnlyList<Engine> engines, Vector2 shipForward, Vector2 centerOfMass,
            float maxLeverArm, in SasTurnInputSettings settings)
        {
            UpdateDesiredHeadingOnTurnRelease(requestedTurnInput, currentHeadingDegrees, settings);

            var headingHoldTurnInput = GetHeadingHoldTurnInput(requestedTurnInput, selfRigidbody, currentHeadingDegrees,
                settings);
            if (Mathf.Abs(requestedTurnInput) > settings.TurnReleaseThreshold ||
                Mathf.Abs(forwardInput) <= Mathf.Epsilon)
                return headingHoldTurnInput;

            var forwardCompensation = CalculateForwardThrustCompensationTurnInput(forwardInput, engines, shipForward,
                centerOfMass, maxLeverArm, settings);
            var withForwardCompensation = headingHoldTurnInput +
                                          forwardCompensation * settings.ForwardCompensationStrength;

            return Mathf.Clamp(withForwardCompensation, -settings.MaxTurnInput, settings.MaxTurnInput);
        }

        private void UpdateDesiredHeadingOnTurnRelease(float requestedTurnInput, float currentHeadingDegrees,
            in SasTurnInputSettings settings)
        {
            CaptureCurrentHeadingIfNeeded(currentHeadingDegrees);

            var isTurning = Mathf.Abs(requestedTurnInput) > settings.TurnReleaseThreshold;
            if (_wasTurning && !isTurning)
                CaptureDesiredHeading(currentHeadingDegrees);

            _wasTurning = isTurning;
        }

        private float GetHeadingHoldTurnInput(float requestedTurnInput, Rigidbody2D selfRigidbody,
            float currentHeadingDegrees, in SasTurnInputSettings settings)
        {
            if (Mathf.Abs(requestedTurnInput) > settings.TurnReleaseThreshold)
                return requestedTurnInput;

            CaptureCurrentHeadingIfNeeded(currentHeadingDegrees);

            var headingError = Mathf.DeltaAngle(currentHeadingDegrees, _desiredHeadingDegrees);
            if (Mathf.Abs(headingError) < settings.HeadingDeadZoneDegrees)
                headingError = 0f;

            var angularVelocityDamping = -selfRigidbody.angularVelocity * settings.AngularVelocityGain;
            var turnCorrection = headingError * settings.HeadingGain + angularVelocityDamping;

            return Mathf.Clamp(turnCorrection, -settings.MaxTurnInput, settings.MaxTurnInput);
        }

        private float CalculateForwardThrustCompensationTurnInput(float forwardInput, IReadOnlyList<Engine> engines,
            Vector2 shipForward, Vector2 centerOfMass, float maxLeverArm, in SasTurnInputSettings settings)
        {
            if (engines.Count == 0)
                return 0f;

            const float sampleTurnInput = 0.2f;

            var baselineTorque = _engineDirectionSolver.EstimateNetTorqueForTurnInput(engines, shipForward,
                centerOfMass, maxLeverArm, forwardInput, 0f);
            if (Mathf.Abs(baselineTorque) <= 0.0001f)
                return 0f;

            var positiveSampleTorque = _engineDirectionSolver.EstimateNetTorqueForTurnInput(engines, shipForward,
                centerOfMass, maxLeverArm, forwardInput, sampleTurnInput);
            var negativeSampleTorque = _engineDirectionSolver.EstimateNetTorqueForTurnInput(engines, shipForward,
                centerOfMass, maxLeverArm, forwardInput, -sampleTurnInput);

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