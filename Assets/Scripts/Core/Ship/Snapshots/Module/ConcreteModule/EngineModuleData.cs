using System;
using Core.Ship.Snapshots.PixelatedRigidbody;

namespace Core.Ship.Snapshots.Module.ConcreteModule
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