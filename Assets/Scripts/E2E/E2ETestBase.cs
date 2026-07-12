using System;
using System.Collections;
using System.Collections.Generic;
using Core.Constants;
using Core.Services;
using Editor.Standalone;
using Events.Gameplay.Collision;
using Events.Gameplay.Ship;
using Events.Gameplay.Shooting;
using Gameplay.EasyTeam;
using Instantiation;
using NUnit.Framework;
using Services;
using ShipFactory.Models;
using Ships;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Systems.Sensing;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Mocks;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;
using Resources = Core.Ships.Resources;

namespace E2E
{
    public abstract class E2ETestBase
    {
        protected readonly List<GameObject> CreatedObjects = new();
        private NavigationService _navigationService;
        private ProjectilesSpawner _projectilesSpawner;

        private ShipService _shipService;
        private GameObject _testRoot;
        protected DiContainer Container;
        protected Instantiator Instantiator;

        [SetUp]
        public virtual void SetUp()
        {
            _testRoot = new GameObject("TestRoot");
            CreatedObjects.Add(_testRoot);

            Container = new DiContainer();

            // Add camera
            var cameraObj = new GameObject("Camera");
            CreatedObjects.Add(cameraObj);
            cameraObj.transform.SetParent(_testRoot.transform);
            cameraObj.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObj.AddComponent<Camera>();
            camera.transform.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 100f;

            // Bind basic event channels
            var collisionEventChannel = ScriptableObject.CreateInstance<CollisionEventChannelSO>();
            Container.Bind<CollisionEventChannelSO>().FromInstance(collisionEventChannel).AsSingle();

            var shootingEventChannel = ScriptableObject.CreateInstance<ShootingEventChannel>();
            Container.Bind<ShootingEventChannel>().FromInstance(shootingEventChannel).AsSingle();

            var testDebrisSpawner = new TestDebrisSpawner();
            Container.Bind<IDebrisSpawner>().FromInstance(testDebrisSpawner).AsSingle();

            var mapInfo = new TestMapInfo(_testRoot.transform);
            Container.Bind<IMapInfo>().FromInstance(mapInfo).AsSingle();

            var shipInitializeModulesEventChannel = new GameObject("Initialize Modules Event Channel")
                .AddComponent<ShipInitializeModulesEventChannel>();
            Container.Bind<ShipInitializeModulesEventChannel>().FromInstance(shipInitializeModulesEventChannel)
                .AsSingle();
            CreatedObjects.Add(shipInitializeModulesEventChannel.gameObject);

            // Bind ShipService
            var shipServiceGo = new GameObject("ShipService");
            CreatedObjects.Add(shipServiceGo);
            _shipService = shipServiceGo.AddComponent<ShipService>();
            Container.Bind<IShipService>().FromInstance(_shipService).AsSingle();

            // Bind NavigationService
            var navigationServiceGo = new GameObject("NavigationService");
            CreatedObjects.Add(navigationServiceGo);
            _navigationService = navigationServiceGo.AddComponent<NavigationService>();
            _navigationService.InternalSectorSize = 20f;
            Container.Bind<INavigationService>().FromInstance(_navigationService).AsSingle();
            var sectorVisualizer = navigationServiceGo.AddComponent<SectorVisualizer>();
            sectorVisualizer.InternalNavigationService = _navigationService;

            // Bind Instantiator & ProjectilesSpawner
            var instantiatorGo = new GameObject("ZenjectInstantiator");
            CreatedObjects.Add(instantiatorGo);
            Instantiator = instantiatorGo.AddComponent<ZenjectInstantiator>();

            var projectilesSpawnerGo = new GameObject("ProjectilesSpawner");
            CreatedObjects.Add(projectilesSpawnerGo);
            _projectilesSpawner = projectilesSpawnerGo.AddComponent<ProjectilesSpawner>();
            _projectilesSpawner.InternalInstantiator = Instantiator;
            _projectilesSpawner.InternalProjectilesHolder = _testRoot.transform;
            Container.Bind<IProjectilesSpawner>().FromInstance(_projectilesSpawner).AsSingle();

            // Bind EffectsSpawner

            var effectsSpawnerGo = new GameObject("EffectsSpawner");
            CreatedObjects.Add(effectsSpawnerGo);
            effectsSpawnerGo.SetActive(false);
            var effectsSpawner = effectsSpawnerGo.AddComponent<EffectsSpawner>();
            effectsSpawner.SetupForTesting(GetExplosionPrefab(), _testRoot.transform, Instantiator);
            effectsSpawnerGo.SetActive(true);
            Container.Bind<IEffectsSpawner>().FromInstance(effectsSpawner).AsSingle();

            var shipModuleCatalog = ScriptableObject.CreateInstance<ShipModuleCatalog>();
            Container.Bind<IShipModuleCatalog>().FromInstance(shipModuleCatalog).AsSingle();

            SnapshotRestoreServicesFactory.Bind(Container, CreatedObjects);

            InjectAllObjectsInScene(Container);
        }

        [TearDown]
        public virtual void TearDown()
        {
            foreach (var obj in CreatedObjects)
                Object.DestroyImmediate(obj);
            CreatedObjects.Clear();
        }

        protected static IEnumerator WaitForLifecycle()
        {
            yield return null;
            yield return null;
        }

        protected static IEnumerator SimulateForSeconds(float seconds, Action onFixedStep = null)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
                onFixedStep?.Invoke();
            }
        }

        protected Team CreateTeam(string name, int layer)
        {
            var teamGo = new GameObject(name);
            CreatedObjects.Add(teamGo);
            var team = teamGo.AddComponent<Team>();
            team.treatNonAlliedAsEnemy = true;
            team.SetLayerName(layer);
            return team;
        }

        protected AIShip CreateAIShip(string name, Team team, Vector2 position, bool withWeapons,
            bool withEngines)
        {
            var shipGo = UnityBuilder.CreateGameObject(name, CreatedObjects, Container);
            shipGo.layer = team.Layer;
            shipGo.transform.position = position;

            const int modulePixelSize = 5;
            const float moduleSpacing = 5f;
            const float engineMaxThrust = 1.6f;

            // Command
            ModuleFactory.CreateCommandModule(shipGo.transform, Vector2.zero, Container, CreatedObjects,
                modulePixelSize, modulePixelSize);
            // Power
            ModuleFactory.CreateTestPowerModule(shipGo.transform, new Vector2(0f, moduleSpacing), Container,
                CreatedObjects, modulePixelSize, modulePixelSize);
            if (withEngines)
            {
                // Engines
                ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(moduleSpacing, 0f), Container,
                    CreatedObjects, engineMaxThrust, modulePixelSize, modulePixelSize, gimbalRange: 180f);
                ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(-moduleSpacing, 0f), Container,
                    CreatedObjects, engineMaxThrust, modulePixelSize, modulePixelSize, gimbalRange: 180f);
            }

            var bulletPrefab = GetBulletPrefab();

            if (withWeapons)
            {
                // Weapon (Cannon)
                var cannonGo = ModuleFactory.CreateModuleBase("Cannon", shipGo.transform,
                    new Vector2(0f, -moduleSpacing), 0f, Container, CreatedObjects, 5, 5);
                var cannon = cannonGo.AddComponent<Cannon>();
                cannon.SetResources(new Resources(0, 1f, 0, 0, 0));
                var weaponSprite = CreateTestSprite();
                var projectileSpawnGo = new GameObject("ProjectileSpawn");
                projectileSpawnGo.transform.SetParent(cannonGo.transform);
                projectileSpawnGo.transform.position = cannonGo.transform.position;
                cannon.SetupForTesting(bulletPrefab, 0.5f, 0.5f, weaponSprite,
                    new List<Transform> { projectileSpawnGo.transform });
            }

            shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ShipSensing>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<AIShip>();
            Container.InjectGameObject(shipGo);
            shipGo.SetActive(true);

            ship.ConfigureSASSettingsForTesting(new SASTurnInputSettings());

            ship.SetTeam(team);
            ship.SetNavigationSize(15);
            ship.ConfigureAllocatorForTesting(true, 14, 1f, 0.4f, 0.02f);
            ship.InitializeModules();

            ship.InternalStopDistance = 4.0f;

            return ship;
        }

        private static GameObject GetBulletPrefab()
        {
            var bulletPrefab = UnityEngine.Resources.Load<GameObject>("Tests/Prefabs/Bullet");

            return bulletPrefab;
        }

        protected static GameObject GetAsteroidPrefab()
        {
            var asteroidPrefab = UnityEngine.Resources.Load<GameObject>("Tests/Prefabs/Asteroid");

            return asteroidPrefab;
        }

        private static GameObject GetExplosionPrefab()
        {
            var explosionPrefab = UnityEngine.Resources.Load<GameObject>("Tests/Prefabs/Explosion");

            return explosionPrefab;
        }

        protected void CreateObstacleBox(Vector3 position, Vector2 size)
        {
            var sizeHalf = size / 2f;

            CreateObstacleWall("ObstacleTop", position + new Vector3(0f, sizeHalf.y, 0f), new Vector2(size.x, 5f));
            CreateObstacleWall("ObstacleBottom", position - new Vector3(0f, sizeHalf.y, 0f), new Vector2(size.x, 5f));
            CreateObstacleWall("ObstacleLeft", position - new Vector3(sizeHalf.x, 0f, 0f), new Vector2(5f, size.y));
            CreateObstacleWall("ObstacleRight", position + new Vector3(sizeHalf.x, 0f, 0f), new Vector2(5f, size.y));
        }

        protected GameObject CreateObstacleWall(string name, Vector2 position, Vector2 size)
        {
            var wall = new GameObject(name);
            CreatedObjects.Add(wall);
            wall.transform.position = position;
            wall.layer = PhysicsLayers.Obstacles;

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;

            // Give it a dummy rigidbody so that collision resolver handles it as a static obstacle
            var rb = wall.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            return wall;
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static void InjectAllObjectsInScene(DiContainer container)
        {
            var allBehaviours = Object.FindObjectsByType<MonoBehaviour>();

            foreach (var behaviour in allBehaviours)
                if (behaviour != null)
                    container.Inject(behaviour);

            Debug.Log("Injected dependencies into all MonoBehaviours in the scene.");
        }
    }
}