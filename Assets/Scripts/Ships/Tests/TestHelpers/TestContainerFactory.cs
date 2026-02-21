using Core.Services;
using Events.Collision;
using Services;
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

            var mapInfo = new TestMapInfo(mapTransform);
            container.Bind<IMapInfo>().FromInstance(mapInfo).AsSingle();

            var shipService = new GameObject("ShipService").AddComponent<ShipService>();
            container.Bind<IShipService>().FromInstance(shipService).AsSingle();

            return container;
        }
    }
}