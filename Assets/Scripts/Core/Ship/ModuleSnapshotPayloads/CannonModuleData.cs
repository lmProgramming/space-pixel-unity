using System;

namespace Core.Ship.ModuleSnapshotPayloads
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