using System;
using UnityEngine;

namespace Core.Ships.Snapshots.Module.ModuleData
{
    [Serializable]
    public class CannonModuleData
    {
        public float reloadTime;
        public float projectileSpeed;
        public string projectileContentId;
        public string spriteContentId;
        public Vector2[] projectileLocalSpawnPoints;
    }
}