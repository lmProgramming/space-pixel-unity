using Core.Services;
using Events.Collision;
using Events.Ship;
using NSubstitute;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers
{
    public static class TestContainerFactory
    {
        public static DiContainer CreateTestContainer(Transform mapTransform)
        {
            var container = new DiContainer();

            var collisionEventChannel = ScriptableObject.CreateInstance<CollisionEventChannelSO>();
            container.Bind<CollisionEventChannelSO>().FromInstance(collisionEventChannel).AsSingle();

            var testDebrisSpawner = new TestDebrisSpawner();
            container.Bind<IDebrisSpawner>().FromInstance(testDebrisSpawner).AsSingle();

            var mapInfo = Substitute.For<IMapInfo>();
            container.Bind<IMapInfo>().FromInstance(mapInfo).AsSingle();

            var shipService = Substitute.For<IShipService>();
            container.Bind<IShipService>().FromInstance(shipService).AsSingle();

            var shipInitializeModulesEventChannel = Substitute.For<ShipInitializeModulesEventChannel>();
            container.Bind<ShipInitializeModulesEventChannel>().FromInstance(shipInitializeModulesEventChannel)
                .AsSingle();

            return container;
        }
    }
}