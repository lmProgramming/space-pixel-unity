using System;
using Core.Ships.Snapshots.PixelatedRigidbody;

namespace Core.Ships.Snapshots.Module.ConcreteModule
{
    [Serializable]
    public class EngineModuleData
    {
        public float maxThrust;
        public float maxGimbalAngle;
        public float gimbalSpeed;
        public PixelatedRigidbodySnapshot[] nozzles;
    }
}