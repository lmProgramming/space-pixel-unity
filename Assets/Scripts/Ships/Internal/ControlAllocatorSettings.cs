namespace Ships.Internal
{
    public readonly struct ControlAllocatorSettings
    {
        public ControlAllocatorSettings(int iterations, float forceWeight, float torqueWeight,
            float regularization)
        {
            Iterations = iterations;
            ForceWeight = forceWeight;
            TorqueWeight = torqueWeight;
            Regularization = regularization;
        }

        public int Iterations { get; }
        public float ForceWeight { get; }
        public float TorqueWeight { get; }
        public float Regularization { get; }
    }
}