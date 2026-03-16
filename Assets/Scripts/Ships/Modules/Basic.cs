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
            if (moduleType != ModuleType.Resources && moduleType != ModuleType.Structural)
            {
                Debug.LogError("Basic module can only be of type Resources or Structural.");
                moduleType = ModuleType.Resources;
            }

            Type = moduleType;
        }
    }
}