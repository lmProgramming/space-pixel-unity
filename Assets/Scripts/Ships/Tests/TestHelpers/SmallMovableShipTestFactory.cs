using System.Collections.Generic;
using Ships.ModuleConnection;
using Ships.Systems.Resources;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers
{
    public static class SmallMovableShipTestFactory
    {
        private const int ModulePixelSize = 5;
        private const float ModuleSpacing = 5f;
        private const float EngineMaxThrust = 800f;

        public static MovableShipTestProxy Create(DiContainer container, ICollection<GameObject> createdObjects)
        {
            var shipGo = ModuleFactory.CreateGameObject("GameplayTestShip", createdObjects);

            ModuleFactory.CreateCommandModule(shipGo.transform, Vector2.zero, container, createdObjects,
                ModulePixelSize, ModulePixelSize);
            ModuleFactory.CreatePowerModule(shipGo.transform, new Vector2(0f, ModuleSpacing), container,
                createdObjects, ModulePixelSize, ModulePixelSize);
            ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(ModuleSpacing, 0f), container,
                createdObjects, EngineMaxThrust, ModulePixelSize, ModulePixelSize);
            ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(-ModuleSpacing, 0f), container,
                createdObjects, EngineMaxThrust, ModulePixelSize, ModulePixelSize);

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<MovableShipTestProxy>();
            container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;
            ship.ConfigureAllocatorForTesting(true, 14, 1f, 0.4f,
                0.02f);
            shipGo.SetActive(true);

            return ship;
        }
    }
}