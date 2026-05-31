using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public sealed class TestModuleCatalog : IShipModuleCatalog
    {
        public bool TryGetModulePrefab(string archetypeId, out GameObject prefab)
        {
            prefab = null;
            return false;
        }
    }
}