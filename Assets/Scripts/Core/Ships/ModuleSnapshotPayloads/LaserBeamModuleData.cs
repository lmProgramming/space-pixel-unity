using System;

namespace Core.Ships.ModuleSnapshotPayloads
{
    [Serializable]
    public class LaserBeamModuleData
    {
        public float reloadTime;
        public float beamRange;
        public string spriteContentId;
    }
}