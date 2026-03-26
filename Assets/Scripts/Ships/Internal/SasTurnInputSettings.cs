namespace Ships.Internal
{
    public readonly struct SasTurnInputSettings
    {
        public SasTurnInputSettings(float turnReleaseThreshold, float headingDeadZoneDegrees, float headingGain,
            float angularVelocityGain, float maxTurnInput, float forwardCompensationStrength,
            float forwardCompensationMaxTurnInput)
        {
            TurnReleaseThreshold = turnReleaseThreshold;
            HeadingDeadZoneDegrees = headingDeadZoneDegrees;
            HeadingGain = headingGain;
            AngularVelocityGain = angularVelocityGain;
            MaxTurnInput = maxTurnInput;
            ForwardCompensationStrength = forwardCompensationStrength;
            ForwardCompensationMaxTurnInput = forwardCompensationMaxTurnInput;
        }

        public float TurnReleaseThreshold { get; }
        public float HeadingDeadZoneDegrees { get; }
        public float HeadingGain { get; }
        public float AngularVelocityGain { get; }
        public float MaxTurnInput { get; }
        public float ForwardCompensationStrength { get; }
        public float ForwardCompensationMaxTurnInput { get; }
    }
}

