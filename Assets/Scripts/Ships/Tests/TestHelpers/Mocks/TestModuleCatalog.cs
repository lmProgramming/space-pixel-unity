using System.Collections.Generic;
using Core.Services;
using UnityEngine;

namespace Ships.Tests.TestHelpers.Mocks
{
    public sealed class TestModuleCatalog : IShipModuleCatalog
    {
        private readonly Dictionary<string, GameObject> _idToPrefab = new();

        public bool TryGetModulePrefab(string archetypeId, out GameObject prefab)
        {
            return _idToPrefab.TryGetValue(archetypeId, out prefab);
        }

        public void Add(string id, GameObject prefab)
        {
            _idToPrefab[id] = prefab;
        }
    }
}