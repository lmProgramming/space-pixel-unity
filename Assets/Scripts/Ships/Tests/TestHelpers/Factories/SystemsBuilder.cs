using System.Collections.Generic;
using Core.Ships;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers.Factories
{
    public static class SystemsBuilder
    {
        public static GameObject CreateNozzleParticleSystem(DiContainer container,
            ICollection<GameObject> createdObjects,
            GameObject nozzleGo)
        {
            var particleParent = UnityBuilder.CreateGameObject("EngineExhaustParent", createdObjects, container);
            particleParent.transform.SetParent(nozzleGo.transform, false);
            var identity = particleParent.AddComponent<GameObjectInstanceIdentity>();
            identity.EnsureAssigned(InstanceOrigin.CatalogPrefab, "engine_exhaust");

            var particleRoot = UnityBuilder.CreateGameObject("EngineExhaust", createdObjects, container);
            particleRoot.transform.SetParent(particleParent.transform, false);
            particleRoot.AddComponent<ParticleSystem>();

            return particleParent;
        }
    }
}