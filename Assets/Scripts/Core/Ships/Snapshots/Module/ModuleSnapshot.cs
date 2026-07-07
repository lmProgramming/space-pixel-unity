using System;
using Core.Ships.Snapshots.PixelatedRigidbody;
using UnityEngine;

namespace Core.Ships.Snapshots.Module
{
    [Serializable]
    public class ModuleSnapshot
    {
        public string instanceId;
        public string moduleName;
        public ModuleType moduleType;
        public string moduleTypeName;
        public InstanceOrigin origin;
        public string archetypeId;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Resources resources;

        public string typePayloadJson;

        [SerializeReference]
        public PixelatedRigidbodySnapshot pixelatedRigidbody;

        public ModuleSnapshot()
        {
        }

        public ModuleSnapshot(string instanceIdValue, string name, ModuleType type, string typeName)
        {
            instanceId = instanceIdValue;
            moduleName = name;
            moduleType = type;
            moduleTypeName = typeName;
        }
    }
}