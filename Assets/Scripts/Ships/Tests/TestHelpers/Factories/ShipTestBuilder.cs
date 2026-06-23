using System.Collections.Generic;
using Core.Ship;
using Pixelation;
using Ships.Modules;
using Ships.Tests.TestHelpers.Proxies;
using UnityEngine;
using Zenject;
using Resources = Core.Ship.Resources;

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
        private readonly DiContainer _container;
        private readonly ICollection<GameObject> _createdObjects;
        private readonly List<Engine> _engines = new();
        private readonly Dictionary<string, Module> _modulesByName = new();
        private readonly List<Module> _otherModules = new();
        private readonly GameObject _shipGo;
        private Module _commandModule;
        private bool _deactivateBeforeWire;
        private bool _injectGameObject = true;

        private ShipTestBuilder(DiContainer container, ICollection<GameObject> createdObjects, string shipName)
        {
            _container = container;
            _createdObjects = createdObjects;
            _shipGo = ModuleFactory.CreateGameObject(shipName, createdObjects, container);
        }

        public static ShipTestBuilder CreateShip(DiContainer container, ICollection<GameObject> createdObjects,
            string shipName = "TestShip")
        {
            return new ShipTestBuilder(container, createdObjects, shipName);
        }

        public ShipTestBuilder ParentedTo(Transform parent, bool deactivateBeforeWire = true)
        {
            _shipGo.transform.SetParent(parent);
            if (deactivateBeforeWire)
                _shipGo.SetActive(false);
            return this;
        }

        public ShipTestBuilder WithoutGameObjectInjection()
        {
            _injectGameObject = false;
            return this;
        }

        public ShipTestBuilder WithCommand(string name, Vector2 localPosition, int width, int height)
        {
            var moduleGo = ModuleFactory.CreateModuleBase(name, _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);
            var command = ModuleFactory.AddCommandModuleComponent(moduleGo);
            RegisterCommand(name, command);
            return this;
        }

        public ShipTestBuilder WithModule(string name, Vector2 localPosition, int width, int height,
            ModuleType type = ModuleType.Resources)
        {
            var moduleGo = ModuleFactory.CreateModuleBase(name, _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);
            var module = ModuleFactory.AddTestModuleComponent(moduleGo, type);
            RegisterOtherModule(name, module);
            return this;
        }

        public ShipTestBuilder WithCrewModule(string name, Vector2 localPosition, int width, int height,
            int crewNeeded, CrewSkillType mainSkill)
        {
            var moduleGo = ModuleFactory.CreateModuleBase(name, _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);
            var testModule = ModuleFactory.AddTestModuleComponent(moduleGo);
            testModule.SetMainSkillType(mainSkill);
            testModule.SetResources(new Resources(0, 0, crewNeeded, 0, 0));
            RegisterOtherModule(name, testModule);
            return this;
        }

        public ShipTestBuilder WithEngineModule(Vector2 localPosition, float maxThrust, int width, int height,
            float rotationZ = 0f, float gimbalRange = 45f)
        {
            ModuleFactory.CreateEngineModule(_shipGo.transform, localPosition, _container, _createdObjects, maxThrust,
                width, height, rotationZ, gimbalRange);
            var engine = _shipGo.transform.GetChild(_shipGo.transform.childCount - 1).GetComponent<Engine>();
            _engines.Add(engine);
            return this;
        }

        public ShipTestBuilder WithPowerModule(Vector2 localPosition, int width, int height)
        {
            ModuleFactory.CreatePowerModule(_shipGo.transform, localPosition, _container, _createdObjects, width,
                height);
            return this;
        }

        public ShipTestBuilder WithCustomCommand(Vector2 localPosition, int width, int height)
        {
            var commandGo = ModuleFactory.CreateModuleBase("Command", _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);
            var command = ModuleFactory.AddCommandModuleComponent(commandGo);
            command.SetResources(new Resources(0, 0, 0, 0, 0));
            var identity = commandGo.AddComponent<ModuleInstanceIdentity>();
            identity.EnsureAssigned(ModuleOrigin.Custom);
            RegisterCommand("Command", command);
            return this;
        }

        public ShipTestBuilder WithCustomEngine(Vector2 localPosition, int width, int height, Resources resources)
        {
            var engineGo = ModuleFactory.CreateModuleBase("Engine", _shipGo.transform, localPosition, 0f, _container,
                _createdObjects, width, height);
            var exhaustRoot = ModuleFactory.CreateGameObject("Exhaust", _createdObjects, _container);
            exhaustRoot.transform.SetParent(engineGo.transform, false);
            exhaustRoot.AddComponent<ParticleSystem>();
            var engine = engineGo.AddComponent<Engine>();
            engine.SetResources(resources);
            var identity = engineGo.AddComponent<ModuleInstanceIdentity>();
            identity.EnsureAssigned(ModuleOrigin.Custom);
            RegisterOtherModule("Engine", engine);
            return this;
        }

        public ShipTestBuilder WithInstantiatedCannon(GameObject cannonPrefab, Vector2 localPosition,
            string archetypeId,
            int pixelSize)
        {
            var cannonInstance = Object.Instantiate(cannonPrefab, _shipGo.transform);
            cannonInstance.transform.localPosition = localPosition;
            cannonInstance.SetActive(true);
            var cannonPixelRb = cannonInstance.GetComponent<PixelatedRigidbody>();
            cannonPixelRb.SetTextureFromColors(ModuleFactory.CreateSolidPixelGrid(pixelSize, pixelSize));
            var cannonIdentity = cannonInstance.GetComponent<ModuleInstanceIdentity>();
            if (cannonIdentity == null)
                cannonIdentity = cannonInstance.AddComponent<ModuleInstanceIdentity>();
            cannonIdentity.EnsureAssigned(ModuleOrigin.CatalogPrefab, archetypeId);
            RegisterOtherModule("Cannon", cannonInstance.GetComponent<Cannon>());
            return this;
        }

        public Module GetModule(string name)
        {
            return _modulesByName[name];
        }

        public ShipLayoutResult BuildLayoutResult(bool initializeModules = false)
        {
            var ship = WireShip<Ship>(initializeModules);
            return new ShipLayoutResult
            {
                Ship = ship,
                CommandModule = _commandModule,
                OtherModules = new List<Module>(_otherModules)
            };
        }

        public Ship Build(bool initializeModules = false)
        {
            return WireShip<Ship>(initializeModules);
        }

        public T Build<T>(bool initializeModules = false) where T : Ship
        {
            return WireShip<T>(initializeModules);
        }

        public ShipWithEnginesResult<ShipTestProxy> BuildWithEnginesResult()
        {
            var ship = WireShip<ShipTestProxy>(false);
            return new ShipWithEnginesResult<ShipTestProxy> { Ship = ship, Engines = new List<Engine>(_engines) };
        }

        public CommandWithEngineResult BuildCommandWithEngineResult()
        {
            var ship = WireShip<Ship>(false);
            return new CommandWithEngineResult
            {
                Ship = ship,
                Command = _shipGo.GetComponentInChildren<Command>(),
                Engine = _shipGo.GetComponentInChildren<Engine>()
            };
        }

        public static GameObject CreateCannonPrefab(DiContainer container, ICollection<GameObject> createdObjects,
            Transform parent, GameObject projectilePrefab, Sprite weaponSprite, int pixelSize = 5)
        {
            var go = ModuleFactory.CreateModuleBase("CannonPrefab", parent, Vector2.zero, 0f,
                container, createdObjects, pixelSize, pixelSize);
            go.SetActive(false);
            var cannon = go.AddComponent<Cannon>();
            cannon.SetResources(new Resources(0, 1, 0, 0, 0));

            var projectileSpawnGo = new GameObject("ProjectileSpawn");
            projectileSpawnGo.transform.SetParent(go.transform);
            projectileSpawnGo.transform.position = go.transform.position;

            cannon.SetupForTesting(projectilePrefab, 1f, 1.5f, weaponSprite, new[] { projectileSpawnGo.transform });

            return go;
        }

        private T WireShip<T>(bool initializeModules) where T : Ship
        {
            var ship = ModuleFactory.WireShip<T>(_shipGo, _container);
            if (initializeModules)
                ship.InitializeModules();
            if (_injectGameObject)
                _container.InjectGameObject(_shipGo);
            return ship;
        }

        private void RegisterCommand(string name, Command command)
        {
            _commandModule = command;
            _modulesByName[name] = command;
        }

        private void RegisterOtherModule(string name, Module module)
        {
            _otherModules.Add(module);
            _modulesByName[name] = module;
        }
    }
}