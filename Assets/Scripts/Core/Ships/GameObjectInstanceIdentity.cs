using System;
using UnityEngine;

namespace Core.Ships
{
    public class GameObjectInstanceIdentity : MonoBehaviour
    {
        [SerializeField] private string instanceId;
        [SerializeField] private InstanceOrigin origin = InstanceOrigin.Custom;
        [SerializeField] private string archetypeId;

        public string InstanceId => instanceId;
        public InstanceOrigin Origin => origin;
        public string ArchetypeId => archetypeId;

        public void EnsureAssigned(InstanceOrigin newOrigin, string newArchetypeId = null)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                instanceId = Guid.NewGuid().ToString("N");

            origin = newOrigin;
            archetypeId = string.IsNullOrWhiteSpace(newArchetypeId) ? string.Empty : newArchetypeId;
        }

        public void RestoreFromSnapshot(string snapshotInstanceId, InstanceOrigin newOrigin, string newArchetypeId)
        {
            instanceId = string.IsNullOrWhiteSpace(snapshotInstanceId)
                ? Guid.NewGuid().ToString("N")
                : snapshotInstanceId;
            origin = newOrigin;
            archetypeId = string.IsNullOrWhiteSpace(newArchetypeId) ? string.Empty : newArchetypeId;
        }
    }
}