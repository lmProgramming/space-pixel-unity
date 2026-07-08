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
        public ConcreteModuleType concreteModuleType;
        public InstanceOrigin origin;
        public string archetypeId;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Resources resources;

        public string typePayloadJson;

        [SerializeReference]
        public PixelatedRigidbodySnapshot pixelatedRigidbody;

        [SerializeReference]
        public StandaloneModuleSystemData.StandaloneModuleSystemData[] systems;

        public ModuleSnapshot()
        {
        }

        public ModuleSnapshot(string instanceIdValue, string name, ConcreteModuleType type)
        {
            instanceId = instanceIdValue;
            moduleName = name;
            concreteModuleType = type;
        }
    }
}