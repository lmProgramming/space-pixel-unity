using System;

namespace Core.Ship.ModuleSnapshotPayloads
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