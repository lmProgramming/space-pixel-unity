using System.Collections.Generic;
using Core.Ship;
using Pixelation;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Systems.Resources;
using Ships.Tests.TestHelpers.Modules;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers.Factories
{
    public static class ModuleFactory
    {
        public static void CreateCommandModule(Transform parent, Vector2 localPosition, DiContainer container,
            ICollection<GameObject> createdObjects,
            int modulePixelWidth, int modulePixelHeight)
        {
            var commandGo = CreateModuleBase("Command", parent, localPosition, 0f, container, createdObjects,
                modulePixelWidth, modulePixelHeight);
            commandGo.AddComponent<Command>();
        }

        public static void CreateTestPowerModule(Transform parent, Vector2 localPosition, DiContainer container,
            ICollection<GameObject> createdObjects,
            int modulePixelWidth, int modulePixelHeight)
        {
            var powerGo = CreateModuleBase("Power", parent, localPosition, 0f, container, createdObjects,
                modulePixelWidth, modulePixelHeight);
            powerGo.AddComponent<TestPowerModule>();
        }

        public static void CreateEngineModule(Transform parent, Vector2 localPosition, DiContainer container,
            ICollection<GameObject> createdObjects, float engineMaxThrust,
            int modulePixelWidth, int modulePixelHeight, float localRotationZ = 0f, float gimbalRange = 45f)
        {
            var engineGo = CreateModuleBase("Engine", parent, localPosition, localRotationZ, container, createdObjects,
                modulePixelWidth, modulePixelHeight);

            AddNozzle(engineGo, container, createdObjects);

            var engine = engineGo.AddComponent<Engine>();
            engine.ConfigureForTesting(engineMaxThrust, gimbalRange);
        }

        public static void AddNozzle(GameObject engineGo, DiContainer container,
            ICollection<GameObject> createdObjects)
        {
            var nozzleGo = CreateGameObject("Nozzle", createdObjects, container);
            nozzleGo.transform.SetParent(engineGo.transform, false);
            nozzleGo.transform.localPosition = new Vector3(0f, 0f, 0f);

            nozzleGo.AddComponent<SpriteRenderer>();

            var nozzleRigidbody = nozzleGo.AddComponent<Rigidbody2D>();
            nozzleRigidbody.bodyType = RigidbodyType2D.Kinematic;
            nozzleRigidbody.gravityScale = 0f;

            nozzleGo.AddComponent<PolygonCollider2D>();

            var particleRoot = CreateGameObject("EngineExhaust", createdObjects, container);
            particleRoot.transform.SetParent(nozzleGo.transform, false);
            particleRoot.AddComponent<ParticleSystem>();

            var nozzle = nozzleGo.AddComponent<Nozzle>();
            container.Inject(nozzle);
            nozzle.SetTextureFromColors(CreateSolidPixelGrid(3, 3));
        }

        public static GameObject CreateModuleBase(string name, Transform parent, Vector2 localPosition,
            float localRotationZ, DiContainer container, ICollection<GameObject> createdObjects,
            int modulePixelWidth, int modulePixelHeight)
        {
            var moduleGo = CreateGameObject(name, createdObjects, container);
            moduleGo.transform.SetParent(parent);
            moduleGo.transform.localPosition = localPosition;
            moduleGo.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

            moduleGo.AddComponent<SpriteRenderer>();

            var rigidbody = moduleGo.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 0f;

            moduleGo.AddComponent<PolygonCollider2D>();

            var pixelatedRb = moduleGo.AddComponent<PixelatedRigidbody>();
            container.Inject(pixelatedRb);
            pixelatedRb.SetTextureFromColors(CreateSolidPixelGrid(modulePixelWidth, modulePixelHeight));

            return moduleGo;
        }

        public static Color32[,] CreateSolidPixelGrid(int width, int height)
        {
            return CreateSolidPixelGrid(width, height, new Color32(100, 100, 100, 255));
        }

        private static Color32[,] CreateSolidPixelGrid(int width, int height, Color32 color)
        {
            var colors = new Color32[width, height];

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                colors[x, y] = color;

            return colors;
        }

        public static T WireShip<T>(GameObject shipGo, DiContainer container) where T : Ship
        {
            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<T>();
            container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;
            shipGo.SetActive(true);

            return ship;
        }

        public static GameObject CreateGameObject(string name, ICollection<GameObject> createdObjects,
            DiContainer container)
        {
            var go = new GameObject(name);
            createdObjects.Add(go);
            container.Inject(go);
            return go;
        }

        public static Command AddCommandModuleComponent(GameObject moduleGo)
        {
            return moduleGo.AddComponent<Command>();
        }

        public static TestModule AddTestModuleComponent(GameObject moduleGo, ModuleType type = ModuleType.Resources)
        {
            var testModule = moduleGo.AddComponent<TestModule>();
            testModule.SetModuleType(type);
            return testModule;
        }
    }
}