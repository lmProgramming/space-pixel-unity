using Core.Ships;
using Ships.ModuleConnection;
using Ships.Modules;
using UnityEngine;
using ZLinq;

namespace Ships
{
    public class StaticModuleConnectionFactory : MonoBehaviour, IModuleConnectionFactory
    {
        public void ConnectModules(IShip ship, Transform shipTransform)
        {
            var modules = shipTransform.GetComponentsInChildren<Module>();
            var graph = ship.ModuleGraph;
            var commandModule = ship.CommandModule;

            foreach (var module in modules)
                graph.AddNode(module);

            foreach (var module in modules.AsValueEnumerable().Where(module => module != (Module)commandModule))
                graph.AddEdge(commandModule, module);

            foreach (var module in modules)
                module.SetShip(ship);
        }
    }
}