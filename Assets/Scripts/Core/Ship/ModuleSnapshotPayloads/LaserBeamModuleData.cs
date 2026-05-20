using System;

namespace Core.Ship.ModuleSnapshotPayloads
{
    [Serializable]
    public class LaserBeamModuleData
    {
        public float reloadTime;
        public float beamRange;
        public string spriteContentId;
    }
}