using System;
using System.Collections.Generic;
using Core.Ships;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using Cysharp.Threading.Tasks;
using Pixelation;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Systems.Standalone;
using Ships.Tests.TestHelpers.Proxies;
using UnityEngine;
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;

namespace Ships.Tests.TestHelpers.Factories
{
    public struct ShipLayoutResult
    {
        public Ship Ship { get; set; }
        public Module CommandModule { get; set; }
        public List<Module> OtherModules { get; set; }
    }

    public sealed class ShipTestBuilder
    {
        private readonly List<Module> _allModules = new();
        private readonly DiContainer _container;
        private readonly ICollection<GameObject> _createdObjects;
        private readonly List<Engine> _engines = new();
        private readonly List<Module> _otherModules = new();
        private readonly GameObject _shipGo;
        private Module _commandModule;
        private bool _deactivateBeforeWire;

        private ShipTestBuilder(DiContainer container, ICollection<GameObject> createdObjects, string shipName)
        {
            _container = container;
            _createdObjects = createdObjects;
            _shipGo = UnityBuilder.CreateGameObject(shipName, createdObjects, container);
        }

        public static ShipTestBuilder CreateShip(DiContainer container, ICollection<GameObject> createdObjects,
            string shipName = "TestShip")
        {
            return new ShipTestBuilder(container, createdObjects, shipName);
        }

        private GameObject CreateModuleBase(string name, Vector2 localPosition, int width, int height)
        {
            return ModuleFactory.CreateModuleBase(name, _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);
        }

        public ShipTestBuilder ParentedTo(Transform parent, bool deactivateBeforeWire = true)
        {
            _shipGo.transform.SetParent(parent);
            if (deactivateBeforeWire)
                _shipGo.SetActive(false);
            return this;
        }

        public ShipTestBuilder WithCommand(string name, Vector2 localPosition, int width, int height)
        {
            var moduleGo = CreateModuleBase(name, localPosition, width, height);

            var command = ModuleFactory.AddCommandModuleComponent(moduleGo);

            RegisterCommand(command);

            return this;
        }

        public ShipTestBuilder WithBasic(string name, Vector2 localPosition, int width, int height,
            ShipResources shipResources)
        {
            var moduleGo = CreateModuleBase(name, localPosition, width, height);

            var basic = ModuleFactory.AddBasicComponent(moduleGo);
            basic.SetResources(shipResources);

            RegisterOtherModule(basic);

            return this;
        }


        public ShipTestBuilder WithLaser(string name, Vector2 localPosition, int width, int height)
        {
            var moduleGo = CreateModuleBase(name, localPosition, width, height);

            var basic = ModuleFactory.AddLaserComponent(moduleGo);

            RegisterOtherModule(basic);

            return this;
        }

        public ShipTestBuilder WithTestModule(string name, Vector2 localPosition, int width, int height,
            ModuleType type = ModuleType.Resources)
        {
            var moduleGo = CreateModuleBase(name, localPosition, width, height);

            var module = ModuleFactory.AddTestModuleComponent(moduleGo, type);

            RegisterOtherModule(module);

            return this;
        }

        public ShipTestBuilder WithTestCrewModule(string name, Vector2 localPosition, int width, int height,
            int crewNeeded, CrewSkillType mainSkill)
        {
            var moduleGo = CreateModuleBase(name, localPosition, width, height);

            var testModule = ModuleFactory.AddTestModuleComponent(moduleGo);
            testModule.SetMainSkillType(mainSkill);
            testModule.SetResources(new ShipResources(0, 0, crewNeeded, 0, 0));

            RegisterOtherModule(testModule);

            return this;
        }

        public ShipTestBuilder WithEngineModule(Vector2 localPosition, float maxThrust, int width, int height,
            float rotationZ = 0f, float gimbalRange = 45f)
        {
            var engine = ModuleFactory.CreateEngineModule(_shipGo.transform, localPosition, _container, _createdObjects,
                maxThrust,
                width, height, rotationZ, gimbalRange);
            _engines.Add(engine);
            return this;
        }

        public ShipTestBuilder WithTestPowerModule(Vector2 localPosition, int width, int height)
        {
            ModuleFactory.CreateTestPowerModule(_shipGo.transform, localPosition, _container, _createdObjects, width,
                height);
            return this;
        }

        public ShipTestBuilder WithCommandOfCustomSnapshotOrigin(Vector2 localPosition, int width, int height)
        {
            var commandGo = ModuleFactory.CreateModuleBase("Command", _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);

            var command = ModuleFactory.AddCommandModuleComponent(commandGo);
            command.SetResources(new ShipResources(0, 0, 0, 0, 0));

            var identity = commandGo.AddComponent<GameObjectInstanceIdentity>();
            identity.EnsureAssigned(InstanceOrigin.Custom);

            RegisterCommand(command);

            return this;
        }

        public ShipTestBuilder WithCustomCommandModule(
            Vector2 localPosition,
            int width,
            int height,
            float rotationZ = 0f,
            Color32[,] colors = null)
        {
            var commandGo = ModuleFactory.CreateModuleBase(
                "Command",
                _shipGo.transform,
                localPosition,
                rotationZ,
                _container,
                _createdObjects,
                width,
                height);

            if (colors != null)
                commandGo.GetComponent<PixelatedRigidbody>().SetTextureFromColors(colors);

            var command = ModuleFactory.AddCommandModuleComponent(commandGo);
            command.SetResources(new ShipResources(0, 0, 0, 0, 0));

            var identity = commandGo.AddComponent<GameObjectInstanceIdentity>();
            identity.EnsureAssigned(InstanceOrigin.Custom);

            RegisterCommand(command);

            return this;
        }

        public ShipTestBuilder WithCustomEngine(Vector2 localPosition, int width, int height,
            ShipResources shipResources)
        {
            var engineGo = ModuleFactory.CreateModuleBase("Engine", _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);

            ModuleFactory.AddNozzle(engineGo, _container, _createdObjects);

            var engine = engineGo.AddComponent<Engine>();
            engine.SetResources(shipResources);

            var identity = engineGo.AddComponent<GameObjectInstanceIdentity>();
            identity.EnsureAssigned(InstanceOrigin.Custom);

            RegisterOtherModule(engine);

            return this;
        }

        public ShipTestBuilder WithInstantiatedModule(string name, GameObject modulePrefab, Vector2 localPosition,
            string archetypeId,
            int pixelSize)
        {
            var moduleInstance = Object.Instantiate(modulePrefab, _shipGo.transform);
            moduleInstance.transform.localPosition = localPosition;
            moduleInstance.SetActive(true);

            moduleInstance.name = name;

            var cannonPixelRb = moduleInstance.GetComponent<PixelatedRigidbody>();
            var module = moduleInstance.GetComponent<Module>();

            if (!cannonPixelRb)
                throw new UnityException(
                    $"[ShipTestBuilder] Module '{name}' does not have a PixelatedRigidbody component.");
            if (!module)
                throw new UnityException($"[ShipTestBuilder] Module '{name}' does not have a Module component.");

            cannonPixelRb.SetTextureFromColors(ModuleFactory.CreateSolidPixelGrid(pixelSize, pixelSize));

            var cannonIdentity = moduleInstance.GetComponent<GameObjectInstanceIdentity>();
            if (cannonIdentity == null)
                cannonIdentity = moduleInstance.AddComponent<GameObjectInstanceIdentity>();
            cannonIdentity.EnsureAssigned(InstanceOrigin.CatalogPrefab, archetypeId);

            RegisterOtherModule(module);

            return this;
        }

        public ShipTestBuilder AddStandaloneModuleSystemToLastModule<T>(StandaloneModuleSystemData data)
            where T : StandaloneModuleSystem
        {
            var lastModule = _allModules.AsValueEnumerable().Last();
            var standaloneSystem = lastModule.gameObject.AddComponent<T>();

            standaloneSystem.RestoreFromSnapshot(data, null);

            return this;
        }

        public ShipLayoutResult BuildLayoutResult(bool initializeModules = false)
        {
            var ship = WireShip<Ship>(initializeModules);
            EnsureAllModulesWired(ship).Forget();

            return new ShipLayoutResult
            {
                Ship = ship,
                CommandModule = _commandModule,
                OtherModules = new List<Module>(_otherModules)
            };
        }

        public DesignShip BuildDesignShip(bool initializeModules = false)
        {
            var ship = WireShip<DesignShip>(initializeModules);
            EnsureAllModulesWired(ship).Forget();

            return ship;
        }

        public Ship Build(bool initializeModules = false)
        {
            var ship = WireShip<Ship>(initializeModules);
            EnsureAllModulesWired(ship).Forget();

            return ship;
        }

        public MovableShipTestProxy BuildMovableProxy(bool initializeModules = false)
        {
            var ship = WireShip<MovableShipTestProxy>(initializeModules);
            EnsureAllModulesWired(ship).Forget();

            ship.ConfigureSASSettingsForTesting(new SASTurnInputSettings());

            return ship;
        }

        public ShipWithEnginesResult<ShipTestProxy> BuildWithEnginesResult()
        {
            var ship = WireShip<ShipTestProxy>(false);
            EnsureAllModulesWired(ship).Forget();

            ship.ConfigureSASSettingsForTesting(new SASTurnInputSettings());

            return new ShipWithEnginesResult<ShipTestProxy> { Ship = ship, Engines = new List<Engine>(_engines) };
        }

        private static async UniTask EnsureAllModulesWired<TShip>(TShip ship) where TShip : Component, IShip
        {
            await UniTask.Yield();

            var allModulesConnected =
                ship.AllModules.Count == ship.GetComponentsInChildren<Module>().Length;

            if (!allModulesConnected)
                throw new ArgumentException("[ShipTestBuilder] Not all modules connected");
        }

        public CommandWithEngineResult BuildCommandWithEngineResult()
        {
            var ship = WireShip<Ship>(false);
            EnsureAllModulesWired(ship).Forget();

            return new CommandWithEngineResult
            {
                Ship = ship,
                Command = _shipGo.GetComponentInChildren<Command>(),
                Engine = _shipGo.GetComponentInChildren<Engine>()
            };
        }

        public ShipTestBuilder WithCannon(GameObject projectilePrefab, Sprite weaponSprite, Vector2 localPosition,
            int width, int height)
        {
            var moduleGo = CreateCannonPrefab(_container, _createdObjects, _shipGo.transform, projectilePrefab,
                weaponSprite, localPosition, width, height);

            moduleGo.SetActive(true);

            RegisterOtherModule(moduleGo.GetComponent<Cannon>());

            return this;
        }

        public static GameObject CreateCannonPrefab(DiContainer container, ICollection<GameObject> createdObjects,
            Transform parent, GameObject projectilePrefab, Sprite weaponSprite, Vector2? localPosition = null,
            int width = 5, int height = 5)
        {
            var go = ModuleFactory.CreateModuleBase("CannonPrefab", parent, localPosition ?? Vector2.zero, 0f,
                container, createdObjects, width, height);
            go.SetActive(false);
            var cannon = go.AddComponent<Cannon>();
            cannon.SetResources(new ShipResources(0, 1, 0, 0, 0));

            var projectileSpawnGo = new GameObject("ProjectileSpawn");
            projectileSpawnGo.transform.SetParent(go.transform);
            projectileSpawnGo.transform.position = go.transform.position;

            cannon.SetupForTesting(projectilePrefab, 1f, 1.5f, weaponSprite,
                new List<Transform> { projectileSpawnGo.transform });

            return go;
        }

        private T WireShip<T>(bool initializeModules) where T : Component, IShip
        {
            var ship = ModuleFactory.WireShip<T>(_shipGo, _container);
            if (initializeModules)
                ship.InitializeModules();
            _container.InjectGameObject(_shipGo);
            return ship;
        }

        private void RegisterCommand(Command command)
        {
            _commandModule = command;
            _allModules.Add(command);
        }

        private void RegisterOtherModule(Module module)
        {
            _otherModules.Add(module);
            _allModules.Add(module);
        }
    }
}