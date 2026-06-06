using System;
using UnityEngine;

namespace Core.Ship
{
    public class ModuleInstanceIdentity : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private ModuleOrigin origin = ModuleOrigin.Custom;
        [SerializeField] private string archetypeId;

        public string InstanceId => instanceId;
        public ModuleOrigin Origin => origin;
        public string ArchetypeId => archetypeId;

        public void EnsureAssigned(ModuleOrigin newOrigin, string newArchetypeId = null)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                instanceId = Guid.NewGuid().ToString("N");

            origin = newOrigin;
            archetypeId = string.IsNullOrWhiteSpace(newArchetypeId) ? string.Empty : newArchetypeId;
        }

        public void RestoreFromSnapshot(string snapshotInstanceId, ModuleOrigin newOrigin, string newArchetypeId)
        {
            instanceId = string.IsNullOrWhiteSpace(snapshotInstanceId)
                ? Guid.NewGuid().ToString("N")
                : snapshotInstanceId;
            origin = newOrigin;
            archetypeId = string.IsNullOrWhiteSpace(newArchetypeId) ? string.Empty : newArchetypeId;
        }
    }
}