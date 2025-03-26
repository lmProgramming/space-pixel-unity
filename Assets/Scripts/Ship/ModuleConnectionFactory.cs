using System;
using System.Collections.Generic;
using Ship.Modules;
using UnityEngine;

namespace Ship
{
    [Serializable]
    public class ModuleData
    {
        public Module module;
        public Vector2Int position;
    }

    public class ModuleConnectionFactory : MonoBehaviour
    {
        [SerializeField] private List<ModuleData> moduleDatas;

        public void ConnectModules(Ship ship)
        {
            for (var i = 0; i < moduleDatas.Count - 1; i++)
            {
                var moduleData = moduleDatas[i];

                for (var j = i + 1; j < moduleDatas.Count; j++)
                {
                    var otherModuleData = moduleDatas[j];

                    moduleData.module.SetupConnections(otherModuleData.module,
                        otherModuleData.module.PixelatedRigidbody.WorldToLocalPixel(otherModuleData.module.transform
                            .position));
                }
            }
        }
    }
}