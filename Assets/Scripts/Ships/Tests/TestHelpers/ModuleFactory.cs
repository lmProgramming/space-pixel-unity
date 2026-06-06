using System.Collections.Generic;
using Pixelation;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Resources;
using UnityEngine;
using Zenject;

namespace Ships.Tests.TestHelpers
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

        public static void CreatePowerModule(Transform parent, Vector2 localPosition, DiContainer container,
            ICollection<GameObject> createdObjects,
            int modulePixelWidth, int modulePixelHeight)
        {
            var powerGo = CreateModuleBase("Power", parent, localPosition, 0f, container, createdObjects,
                modulePixelWidth, modulePixelHeight);
            powerGo.AddComponent<TestPowerModule>();
        }

        public static void CreateEngineModule(Transform parent, Vector2 localPosition, DiContainer container,
            ICollection<GameObject> createdObjects, float engineMaxThrust,
            int modulePixelWidth, int modulePixelHeight, float localRotationZ = 0f)
        {
            var engineGo = CreateModuleBase("Engine", parent, localPosition, localRotationZ, container, createdObjects,
                modulePixelWidth, modulePixelHeight);

            var particleRoot = CreateGameObject("EngineExhaust", createdObjects);
            particleRoot.transform.SetParent(engineGo.transform, false);
            particleRoot.AddComponent<ParticleSystem>();

            var engine = engineGo.AddComponent<Engine>();
            engine.ConfigureForTesting(engineMaxThrust);
        }

        public static GameObject CreateModuleBase(string name, Transform parent, Vector2 localPosition,
            float localRotationZ, DiContainer container, ICollection<GameObject> createdObjects,
            int modulePixelWidth, int modulePixelHeight)
        {
            var moduleGo = CreateGameObject(name, createdObjects);
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

        public static Color32[,] CreateSolidPixelGrid(int width, int height, Color32 color)
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

        public static GameObject CreateGameObject(string name, ICollection<GameObject> createdObjects)
        {
            var go = new GameObject(name);
            createdObjects.Add(go);
            return go;
        }
    }
}