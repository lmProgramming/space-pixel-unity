using System;
using System.Collections;
using System.Collections.Generic;
using Core.Constants;
using Core.Gameplay;
using Core.Services;
using Core.Ships;
using Core.State;
using Events.Gameplay.Ship;
using Gameplay.EasyTeam;
using Instantiation;
using LMPro.External.IsAlive;
using NUnit.Framework;
using Services;
using Ships;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Systems.Sensing;
using Ships.Tests.TestHelpers.Factories;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;

namespace E2E
{
    public abstract class E2ETestBase
    {
        protected readonly List<GameObject> CreatedObjects = new();
        private IActivePlayerShipProvider _activePlayerShipProvider;
        private INavigationService _navigationService;

        private DiContainer _sceneContainer;
        protected Instantiator Instantiator;
        protected IMissionService MissionService;

        [UnitySetUp]
        public IEnumerator SetupScene()
        {
            yield return LoadMainGame();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var obj in CreatedObjects.AsValueEnumerable().Where(obj => obj != null))
                Object.DestroyImmediate(obj);
            CreatedObjects.Clear();

            DestroyEverythingExceptTestRunner();
            ResetSaveState();

            yield return null;
        }

        private IEnumerator LoadMainGame()
        {
            ConfigureEmptyFreeModeSaveState();
            DestroyEverythingExceptTestRunner();

            Assert.That(Application.CanStreamedLevelBeLoaded(SceneNames.MainGame),
                $"Scene '{SceneNames.MainGame}' must be in Build Settings for E2E tests.");

            var loadOp = SceneManager.LoadSceneAsync(SceneNames.MainGame, LoadSceneMode.Single);
            Assert.That(loadOp, Is.Not.Null, $"Failed to start loading '{SceneNames.MainGame}'.");

            loadOp.completed += _ => { Object.FindAnyObjectByType<SkirmishSetup>().SetupMissionService = false; };

            while (!loadOp.isDone)
                yield return null;

            yield return null;

            var sceneContext = Object.FindAnyObjectByType<SceneContext>();
            Assert.That(sceneContext, Is.Not.Null, "MainGame scene is missing a SceneContext.");
            Assert.That(sceneContext.Container, Is.Not.Null, "SceneContext.Container was not initialized.");

            _sceneContainer = sceneContext.Container;
            _sceneContainer.Resolve<IShipService>();
            _activePlayerShipProvider = _sceneContainer.Resolve<IActivePlayerShipProvider>();
            _navigationService = _sceneContainer.Resolve<INavigationService>();
            MissionService = _sceneContainer.Resolve<IMissionService>();
            Instantiator = Object.FindAnyObjectByType<ZenjectInstantiator>();
            Assert.That(Instantiator, Is.Not.Null, "MainGame scene is missing an Instantiator.");

            EnsureShipInitializeModulesEventChannelBound();
        }

        private int GetNavigableShipSize()
        {
            var sectorSize = _navigationService.SectorSize;
            return Mathf.Max(1, Mathf.FloorToInt(sectorSize) - 1);
        }

        private void EnsureShipInitializeModulesEventChannelBound()
        {
            // Real ship shells bind this via GameObjectContext + ShipInstaller.
            // ModuleFactory test ships inject from the scene container instead.
            if (_sceneContainer.HasBinding<ShipInitializeModulesEventChannel>())
                return;

            var channelGo = new GameObject(nameof(ShipInitializeModulesEventChannel));
            CreatedObjects.Add(channelGo);
            var channel = channelGo.AddComponent<ShipInitializeModulesEventChannel>();
            _sceneContainer.Bind<ShipInitializeModulesEventChannel>().FromInstance(channel).AsSingle();
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
            var shipGo = BuildShipShell(name, team, position);
            AddStandardModules(shipGo, withWeapons, withEngines);

            shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ShipSensing>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<AIShip>();
            _sceneContainer.InjectGameObject(shipGo);
            shipGo.SetActive(true);

            ship.ConfigureSASSettingsForTesting(new SASTurnInputSettings());
            ship.SetTeam(team);
            // Footprint must fit the scene NavigationService sector grid (old harness used sector 20 + size 15).
            ship.SetNavigationSize(GetNavigableShipSize());
            ship.ConfigureAllocatorForTesting(true, 14, 1f, 0.4f, 0.02f);
            ship.InitializeModules();
            ship.InternalStopDistance = 4.0f;

            return ship;
        }

        protected PlayerShip CreatePlayerShip(string name, Team team, Vector2 position, bool withWeapons,
            bool withEngines)
        {
            var shipGo = BuildShipShell(name, team, position);
            AddStandardModules(shipGo, withWeapons, withEngines);

            shipGo.AddComponent<ModuleConnectionFactory>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<PlayerShip>();
            _sceneContainer.InjectGameObject(shipGo);
            shipGo.SetActive(true);

            ship.ConfigureSASSettingsForTesting(new SASTurnInputSettings());
            ship.SetTeam(team);
            ship.ConfigureAllocatorForTesting(true, 14, 1f, 0.4f, 0.02f);
            ship.InitializeModules();
            _activePlayerShipProvider.SetActiveShip(ship);

            return ship;
        }

        protected static int CountPixels(IShip ship)
        {
            if (ship == null || !ship.IsAlive())
                return 0;

            return ship.AllModules.AsValueEnumerable()
                .Sum(module => module?.PixelatedRigidbody?.CurrentPixelCount ?? 0);
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
            wall.transform.localScale = size;

            wall.AddComponent<BoxCollider2D>();

            var rb = wall.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var spriteRenderer = wall.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateWhiteSprite();

            return wall;
        }

        private static Sprite CreateWhiteSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var rect = new Rect(0, 0, 1, 1);
            var pivot = new Vector2(0.5f, 0.5f);
            var whitePixelSprite = Sprite.Create(texture, rect, pivot, 1.0f);

            return whitePixelSprite;
        }

        protected static GameObject GetAsteroidPrefab()
        {
            var asteroidPrefab = Resources.Load<GameObject>("Tests/Prefabs/Asteroid");
            return asteroidPrefab == null
                ? throw new UnityException("[E2E] Missing Resources/Tests/Prefabs/Asteroid.")
                : asteroidPrefab;
        }

        private GameObject BuildShipShell(string name, Team team, Vector2 position)
        {
            var shipGo = UnityBuilder.CreateGameObject(name, CreatedObjects, _sceneContainer);
            shipGo.layer = team.Layer;
            shipGo.transform.position = position;
            return shipGo;
        }

        private void AddStandardModules(GameObject shipGo, bool withWeapons, bool withEngines)
        {
            const int modulePixelSize = 5;
            const float moduleSpacing = 5f;
            const float engineMaxThrust = 1.6f;

            ModuleFactory.CreateCommandModule(shipGo.transform, Vector2.zero, _sceneContainer, CreatedObjects,
                modulePixelSize, modulePixelSize);
            ModuleFactory.CreateTestPowerModule(shipGo.transform, new Vector2(0f, moduleSpacing), _sceneContainer,
                CreatedObjects, modulePixelSize, modulePixelSize);

            if (withEngines)
            {
                ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(moduleSpacing, 0f), _sceneContainer,
                    CreatedObjects, engineMaxThrust, modulePixelSize, modulePixelSize, gimbalRange: 180f);
                ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(-moduleSpacing, 0f), _sceneContainer,
                    CreatedObjects, engineMaxThrust, modulePixelSize, modulePixelSize, gimbalRange: 180f);
            }

            if (!withWeapons)
                return;

            var bulletPrefab = GetBulletPrefab();

            var cannonGo = ModuleFactory.CreateModuleBase("Cannon", shipGo.transform,
                new Vector2(0f, -moduleSpacing), 0f, _sceneContainer, CreatedObjects, 5, 5);

            var cannon = cannonGo.AddComponent<Cannon>();
            cannon.SetResources(new ShipResources(0, 1f, 0, 0, 0));

            var weaponSprite = CreateTestSprite();

            var projectileSpawnGo = new GameObject("ProjectileSpawn");
            projectileSpawnGo.transform.SetParent(cannonGo.transform);
            projectileSpawnGo.transform.position = cannonGo.transform.position;

            cannon.SetupForTesting(bulletPrefab, 0.5f, 0.5f, weaponSprite,
                new List<Transform> { projectileSpawnGo.transform });
        }

        private static GameObject GetBulletPrefab()
        {
            var bulletPrefab = Resources.Load<GameObject>("Tests/Prefabs/Bullet");
            return bulletPrefab == null
                ? throw new UnityException("[E2E] Missing Resources/Tests/Prefabs/Bullet.")
                : bulletPrefab;
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }

        private static void ConfigureEmptyFreeModeSaveState()
        {
            SaveState.Mode = GameSessionMode.FreeMode;
            SaveState.PlayerShipName = null;
            SaveState.PlayerShipSnapshotFilePath = null;
            SaveState.EnemyShipCount = 0;
            SaveState.FriendlyShipCount = 0;
            SaveState.AsteroidCount = 0;
            SaveState.ProgressionSlotIndex = 0;
            SaveState.SelectedAllyIndex = 0;
        }

        private static void ResetSaveState()
        {
            ConfigureEmptyFreeModeSaveState();
        }

        private static void DestroyEverythingExceptTestRunner()
        {
            const string testRunnerGameObjectName = "Code-based tests runner";

            var testRunner = GameObject.Find(testRunnerGameObjectName);
            if (testRunner != null)
                Object.DontDestroyOnLoad(testRunner);

            for (var i = 0; i < SceneManager.sceneCount; i++)
                foreach (var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    Object.DestroyImmediate(root);

            if (!ProjectContext.HasInstance)
            {
                StaticContext.Clear();
                return;
            }

            foreach (var root in ProjectContext.Instance.gameObject.scene.GetRootGameObjects())
                if (root.name != testRunnerGameObjectName)
                    Object.DestroyImmediate(root);

            StaticContext.Clear();
        }
    }
}