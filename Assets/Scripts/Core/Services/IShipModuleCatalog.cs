using System.Collections.Generic;
using Core.ShipFactory;
using Core.Ships;
using UnityEngine;

namespace Core.Services
{
    public interface IShipModuleCatalog
    {
        bool TryGetModulePrefab(string archetypeId, out GameObject prefab);

        bool TryGetModuleSO(string archetypeId, out ShipModuleSO moduleSO);

        public IReadOnlyList<ShipModuleSO> GetModuleSOsOfType(ModuleType type);
    }
}