using Ships.Modules;
using UnityEngine;
using ZLinq;

namespace Ships
{
    public class StaticModuleConnectionFactory : MonoBehaviour, IModuleConnectionFactory
    {
        public void ConnectModules(Ship ship)
        {
            var modules = ship.GetComponentsInChildren<Module>();
            var graph = ship.ModuleGraph;
            var commandModule = ship.CommandModule;

            foreach (var module in modules)
                graph.AddNode(module);

            foreach (var module in modules.AsValueEnumerable().Where(module => module != (Module)commandModule))
                graph.AddEdge(commandModule, module);

            foreach (var module in modules)
                module.Setup(ship);
        }
    }
}