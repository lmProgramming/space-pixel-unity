using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Pixelation;
using Ships.Internal;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using Zenject;
using Object = UnityEngine.Object;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipControlAllocatorThrustTests
    {
        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TestRoot");
            _createdObjects.Add(_testRoot);
            _container = TestContainerFactory.CreateTestContainer(_testRoot.transform);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
                if (obj != null)
                    Object.DestroyImmediate(obj);
        }

        private struct EngineSpec
        {
            public Vector2 LocalPosition;
            public float LocalRotationZ;
            public float MaxThrust;
            public Vector2 ThrustPoint;
        }

        private sealed class ShipTestProxy : Ship
        {
            protected override void Move()
            {
            }
        }

        private DiContainer _container;
        private readonly List<GameObject> _createdObjects = new();
        private GameObject _testRoot;

        [UnityTest]
        public IEnumerator ForwardInput_OneRearEngine_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = CreateShipWithEngines(
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, -5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f,
                    ThrustPoint = Vector2.zero
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(1f, 0f, 0.02f,
                false);

            Assert.That(shipWithEngines.Engines[0].CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator BackwardInput_BackwardFacingEngine_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = CreateShipWithEngines(
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, -5f),
                    LocalRotationZ = 180f,
                    MaxThrust = 10f,
                    ThrustPoint = Vector2.zero
                },
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, 5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f,
                    ThrustPoint = Vector2.zero
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(-1f, 0f, 0.02f,
                false);

            Assert.That(shipWithEngines.Engines[0].CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator BackwardInput_ForwardFacingEngine_UsesAtMostTenPercentThrust()
        {
            var shipWithEngines = CreateShipWithEngines(
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, -5f),
                    LocalRotationZ = 180f,
                    MaxThrust = 10f,
                    ThrustPoint = Vector2.zero
                },
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, 5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f,
                    ThrustPoint = Vector2.zero
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(-1f, 0f, 0.02f,
                false);

            Assert.That(shipWithEngines.Engines[1].CurrentThrustRatioForTesting, Is.LessThanOrEqualTo(0.1f));
        }

        private IEnumerator WaitForLifecycle()
        {
            yield return null;
            yield return null;
        }

        private (ShipTestProxy Ship, List<Engine> Engines) CreateShipWithEngines(params EngineSpec[] engineSpecs)
        {
            var shipGo = CreateGameObject("AllocatorTestShip");

            CreateCommandModule(shipGo.transform, Vector2.zero);

            var engines = new List<Engine>();
            foreach (var spec in engineSpecs)
                engines.Add(CreateEngineModule(shipGo.transform, spec));

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();

            shipGo.SetActive(false);
            var ship = shipGo.AddComponent<ShipTestProxy>();
            _container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;
            shipGo.SetActive(true);

            return (ship, engines);
        }

        private void CreateCommandModule(Transform parent, Vector2 localPosition)
        {
            var commandGo = CreateModuleBase("Command", parent, localPosition, 0f);
            commandGo.AddComponent<Command>();
        }

        private Engine CreateEngineModule(Transform parent, EngineSpec spec)
        {
            var engineGo = CreateModuleBase("Engine", parent, spec.LocalPosition, spec.LocalRotationZ);

            var particleRoot = CreateGameObject("EngineExhaust");
            particleRoot.transform.SetParent(engineGo.transform, false);
            particleRoot.AddComponent<ParticleSystem>();

            var engine = engineGo.AddComponent<Engine>();
            engine.ConfigureForTesting(spec.MaxThrust, spec.ThrustPoint);

            return engine;
        }

        private GameObject CreateModuleBase(string name, Transform parent, Vector2 localPosition, float localRotationZ)
        {
            var moduleGo = CreateGameObject(name);
            moduleGo.transform.SetParent(parent);
            moduleGo.transform.localPosition = localPosition;
            moduleGo.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

            moduleGo.AddComponent<SpriteRenderer>();

            var rigidbody = moduleGo.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 0f;

            moduleGo.AddComponent<PolygonCollider2D>();

            var pixelatedRb = moduleGo.AddComponent<PixelatedRigidbody>();
            _container.Inject(pixelatedRb);
            pixelatedRb.SetTextureFromColors(CreateSolidPixelGrid(5, 5));

            return moduleGo;
        }

        private Color32[,] CreateSolidPixelGrid(int width, int height)
        {
            var colors = new Color32[width, height];
            var solidColor = new Color32(100, 100, 100, 255);

            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                colors[x, y] = solidColor;

            return colors;
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }
    }
}