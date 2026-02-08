using Core.Services;
using Events.Collision;
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

            var junkSpawner = new TestJunkSpawner();
            container.Bind<IJunkSpawner>().FromInstance(junkSpawner).AsSingle();

            var mapInfo = new TestMapInfo(mapTransform);
            container.Bind<IMapInfo>().FromInstance(mapInfo).AsSingle();

            var shipService = new TestShipService();
            container.Bind<IShipService>().FromInstance(shipService).AsSingle();

            return container;
        }
    }
}