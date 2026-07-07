using System;

namespace Core.Ships.ModuleSnapshotPayloads
{
    [Serializable]
    public class CannonModuleData
    {
        public float reloadTime;
        public float projectileSpeed;
        public string projectileContentId;
        public string spriteContentId;
    }
}