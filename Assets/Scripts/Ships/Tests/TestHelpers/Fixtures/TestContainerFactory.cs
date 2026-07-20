using System.Collections.Generic;
using Core.Constants;
using Core.Grid;
using Core.Pixelation;
using Core.Services;
using Events.Gameplay.Collision;
using Events.Gameplay.Ship;
using Events.Gameplay.Shooting;
using NSubstitute;
using Pixelation;
using Pixelation.CollisionResolver;
using Ships.Tests.TestHelpers.Mocks;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers.Fixtures
{
    public static class TestContainerFactory
    {
        public static DiContainer CreateTestContainer(
            ICollection<GameObject> createdObjects = null)
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

            var projectilesSpawner = Substitute.For<IProjectilesSpawner>();
            container.Bind<IProjectilesSpawner>().FromInstance(projectilesSpawner).AsSingle();

            var shipInitializeModulesEventChannelGo = new GameObject(nameof(ShipInitializeModulesEventChannel));
            createdObjects?.Add(shipInitializeModulesEventChannelGo);
            var shipInitializeModulesEventChannel =
                shipInitializeModulesEventChannelGo.AddComponent<ShipInitializeModulesEventChannel>();
            container.Bind<ShipInitializeModulesEventChannel>().FromInstance(shipInitializeModulesEventChannel)
                .AsSingle();

            var moduleCatalog = Substitute.For<IShipModuleCatalog>();
            container.Bind<IShipModuleCatalog>()
                .FromInstance(moduleCatalog)
                .AsSingle();

            var pixelatedRigidbodyFactory = Substitute.For<IPixelatedRigidbodyFactory>();
            container.Bind<IPixelatedRigidbodyFactory>()
                .FromInstance(pixelatedRigidbodyFactory)
                .AsSingle();

            var moduleRestoreFactory = Substitute.For<IModuleRestoreFactory>();
            container.Bind<IModuleRestoreFactory>().FromInstance(moduleRestoreFactory).AsSingle();

            var shootingEventChannel = ScriptableObject.CreateInstance<ShootingEventChannel>();
            container.Bind<ShootingEventChannel>().FromInstance(shootingEventChannel).AsSingle();

            var effectsSpawner = Substitute.For<IEffectsSpawner>();
            container.Bind<IEffectsSpawner>().FromInstance(effectsSpawner).AsSingle();

            container.Bind<GameplayConstants>()
                .FromScriptableObjectResource("Tests/GameplayConstants")
                .AsSingle();

            container.BindFactory<ITexturePixelGrid, PixelatedRigidbody, PolygonCollider2D, PixelCollisionHandler,
                PixelCollisionHandler.Factory>();

            container
                .BindFactory<PixelCollisionHandler, IPixelatedRigidbody, PhysicsCollision, PhysicsCollision.Factory>();

            container.BindFactory<PixelCollisionHandler, IPixelatedRigidbody, DestroyCollidingPixel,
                DestroyCollidingPixel.Factory>();

            return container;
        }
    }
}