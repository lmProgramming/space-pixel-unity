using UnityEngine;

namespace Core.Services
{
    public interface IShipModuleCatalog
    {
        bool TryGetModulePrefab(string archetypeId, out GameObject prefab);
    }
}
