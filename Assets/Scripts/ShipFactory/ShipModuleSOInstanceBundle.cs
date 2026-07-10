using Core.Ships.Module;
using UnityEngine;

namespace ShipFactory
{
    public class ShipModuleSOInstanceBundle
    {
        public readonly GameObject Instance;
        public readonly ShipModuleSO ModuleSO;
        public readonly IModule PlacedModule;

        public ShipModuleSOInstanceBundle(GameObject instance, ShipModuleSO moduleSO, IModule placedModule)
        {
            Instance = instance;
            ModuleSO = moduleSO;
            PlacedModule = placedModule;
        }
    }
}