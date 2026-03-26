using Core.Ship;
using UnityEngine;

namespace Ships.Modules
{
    public class Basic : Module
    {
        [SerializeField]
        private ModuleType moduleType;

        public ModuleType ModuleType => moduleType;

        private void OnValidate()
        {
            Type = moduleType;
        }
    }
}