using System.Collections.Generic;
using System.Linq;
using Ships.Modules;
using UnityEngine;

namespace Ships
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

            foreach (var module in modules) ship.ModuleGraph.AddNode(module);

            var graph = ship.ModuleGraph;
            for (var i = 0; i < modules.Count - 1; i++)
            {
                var module = modules[i];

                for (var j = i + 1; j < modules.Count; j++)
                {
                    var otherModule = modules[j];

                    FixedJoint2D joint = null;
                    module.SetupConnections(otherModule, ref joint);
                    otherModule.SetupConnections(module, ref joint);

                    if (!joint) continue;
                    graph.AddNode(module);
                    graph.AddNode(otherModule);

                    graph.AddEdge(module, otherModule);
                }
            }

            foreach (var module in modules) module.Setup(ship);
        }
    }
}