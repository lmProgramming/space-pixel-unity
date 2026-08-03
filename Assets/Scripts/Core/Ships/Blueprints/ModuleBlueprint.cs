using System;
using UnityEngine;

namespace Core.Ships.Blueprints
{
    [Serializable]
    public class ModuleBlueprint
    {
        public string blueprintId;
        public string archetypeId;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public bool removedByPlayer;

        public ModuleBlueprint()
        {
        }

        public ModuleBlueprint(string blueprintIdValue, string archetypeIdValue, Vector3 position, Quaternion rotation)
        {
            blueprintId = blueprintIdValue;
            archetypeId = archetypeIdValue;
            localPosition = position;
            localRotation = rotation;
        }
    }
}