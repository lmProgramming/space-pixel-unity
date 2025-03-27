using System.Collections.Generic;
using System.Linq;
using Ship.Modules;
using UnityEngine;

namespace Ship
{
    public class ModuleConnectionFactory : MonoBehaviour
    {
        private List<Module> GetModules(Transform parent)
        {
            var modules = GetComponentsInChildren<Module>(parent).ToList();
            return modules;
        }

        public void ConnectModules(Ship ship)
        {
            var modules = GetModules(ship.transform);

            for (var i = 0; i < modules.Count - 1; i++)
            {
                var module = modules[i];

                for (var j = i + 1; j < modules.Count; j++)
                {
                    var otherModuleData = modules[j];

                    module.SetupConnections(otherModuleData,
                        otherModuleData.PixelatedRigidbody.WorldToLocalPixel(otherModuleData.transform
                            .position));
                }
            }
        }
    }
}