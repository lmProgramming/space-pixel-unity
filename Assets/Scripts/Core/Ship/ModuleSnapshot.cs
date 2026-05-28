using System;
using UnityEngine;

namespace Core.Ship
{
    [Serializable]
    public abstract class ModuleSnapshotBase
    {
        public string instanceId;
        public string moduleName;
        public ModuleType moduleType;
        public string moduleTypeName;
        public ModuleOrigin origin;
        public string archetypeId;
        public Vector3 localPosition;
        public Quaternion localRotation;

        [SerializeReference]
        public PixelGridSnapshot colorGrid;

        [SerializeReference]
        public ArmorGridSnapshot armorGrid;

        [SerializeReference]
        public HealthGridSnapshot healthGrid;
        public float defaultPixelHealth = 1f;
        public float maxArmorHealth = 10f;
        public Resources resources;

        protected ModuleSnapshotBase()
        {
        }

        protected ModuleSnapshotBase(string instanceIdValue, string name, ModuleType type, string typeName)
        {
            instanceId = instanceIdValue;
            moduleName = name;
            moduleType = type;
            moduleTypeName = typeName;
        }
    }

    [Serializable]
    public class ModuleSnapshot : ModuleSnapshotBase
    {
        public string typePayloadJson;

        public ModuleSnapshot()
        {
        }

        public ModuleSnapshot(string instanceIdValue, string name, ModuleType type, string typeName) :
            base(instanceIdValue, name, type, typeName)
        {
        }
    }
}