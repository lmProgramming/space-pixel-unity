using System;
using UnityEngine;

namespace Core.Ship
{
    [Serializable]
    public class ModuleSnapshot
    {
        public string moduleName;
        public ModuleType moduleType;
        public string moduleTypeName;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public PixelGridSnapshot pixelGrid;
        public Resources resources;

        /// <summary>
        ///     Full JSON of the Module component's serialized fields (reloadTime, projectilePrefab, etc.).
        ///     Captured via JsonUtility.ToJson, restored via JsonUtility.FromJsonOverwrite.
        ///     Object references (prefabs) only survive within the same editor session.
        /// </summary>
        public string moduleComponentJson;

        public ModuleSnapshot()
        {
        }

        public ModuleSnapshot(string name, ModuleType type, string typeName)
        {
            moduleName = name;
            moduleType = type;
            moduleTypeName = typeName;
        }
    }
}