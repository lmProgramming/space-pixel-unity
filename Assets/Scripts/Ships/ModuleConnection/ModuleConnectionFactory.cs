using System.Collections.Generic;
using UnityEngine;
using ZLinq;
using Module = Ships.Modules.Module;

namespace Ships.ModuleConnection
{
    public class ModuleConnectionFactory : MonoBehaviour, IModuleConnectionFactory
    {
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

            foreach (var module in modules) module.SetShip(ship);

            WarnAboutIsolatedModules(ship, modules);
        }

        private static void WarnAboutIsolatedModules(Ship ship, List<Module> modules)
        {
            var graph = ship.ModuleGraph;

            if (graph.GetAllNodes().Count <= 1) return;

            foreach (var module in modules.AsValueEnumerable()
                         .Where(module => graph.GetConnectedNodes(module).Count == 0))
                Debug.LogWarning(
                    $"[ModuleConnectionFactory] Module '{module.name}' on ship '{ship.name}' has no graph edges " +
                    "after ConnectModules. It may be isolated from the rest of the ship.",
                    module);
        }

        private static List<Module> GetModules(Transform parent)
        {
            return parent.GetComponentsInChildren<Module>().AsValueEnumerable().ToList();
        }
    }
}