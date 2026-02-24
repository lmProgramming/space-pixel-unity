using System.Collections;
using Core.Ship;
using NUnit.Framework;
using Pixelation;
using Ships.Internal;
using Ships.Modules;
using Ships.Serialization;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipSnapshotServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            _service = new ShipSnapshotService();
            _root = new GameObject("Root");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        private ShipSnapshotService _service;
        private GameObject _root;

        [Test]
        public void ToJson_AndFromJson_RoundTrip_PreservesShipName()
        {
            var snapshot = new ShipSnapshot("TestShip");
            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            Assert.AreEqual("TestShip", restored.shipName);
        }

        [Test]
        public void ToJson_AndFromJson_RoundTrip_PreservesModuleData()
        {
            var snapshot = new ShipSnapshot("Ship");
            var moduleSnapshot = new ModuleSnapshot("Hull", ModuleType.Command)
            {
                localPosition = new Vector3(1f, 2f, 0f),
                localRotation = Quaternion.identity
            };

            var pg = new PixelGridSnapshot(3, 3);
            pg.SetPixel(1, 1, new Color32(255, 0, 0, 255));

            moduleSnapshot.pixelGrid = pg;
            snapshot.modules.Add(moduleSnapshot);

            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            Assert.AreEqual(1, restored.modules.Count);
            Assert.AreEqual("Hull", restored.modules[0].moduleName);
            Assert.AreEqual(new Vector3(1f, 2f, 0f), restored.modules[0].localPosition);
            Assert.AreEqual(255, restored.modules[0].pixelGrid.GetPixel(1, 1).r);
        }

        [Test]
        public void ToJson_AndFromJson_RoundTrip_PreservesCommandModuleIndex()
        {
            var snapshot = new ShipSnapshot("Ship") { commandModuleIndex = 2 };
            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            Assert.AreEqual(2, restored.commandModuleIndex);
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_RestoresPixelGridAndTransform()
        {
            var container = TestContainerFactory.CreateTestContainer(_root.transform);

            var shipGo = new GameObject("Ship");
            shipGo.SetActive(false);

            var moduleGo = new GameObject("Module");
            moduleGo.transform.SetParent(shipGo.transform);
            moduleGo.AddComponent<SpriteRenderer>();

            var rb2d = moduleGo.AddComponent<Rigidbody2D>();
            rb2d.bodyType = RigidbodyType2D.Dynamic;
            rb2d.gravityScale = 0f;

            moduleGo.AddComponent<PolygonCollider2D>();

            var pixelatedRb = moduleGo.AddComponent<PixelatedRigidbody>();
            container.Inject(pixelatedRb);

            var initialColors = new Color32[3, 3];
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                initialColors[x, y] = new Color32(100, 100, 100, 255);

            pixelatedRb.SetTextureFromColors(initialColors);

            var testModule = moduleGo.AddComponent<TestModule>();
            testModule.SetModuleType(ModuleType.Production);

            var commandGo = new GameObject("Command");
            commandGo.transform.SetParent(shipGo.transform);

            var commandPixelatedRb = commandGo.AddComponent<PixelatedRigidbody>();
            container.Inject(commandPixelatedRb);

            var commandColors = new Color32[3, 3];
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                commandColors[x, y] = new Color32(50, 50, 50, 255);

            commandPixelatedRb.SetTextureFromColors(commandColors);

            commandGo.AddComponent<Command>();

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();

            var ship = shipGo.AddComponent<Ship>();
            container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;

            shipGo.SetActive(true);

            yield return null;

            var pg = new PixelGridSnapshot(3, 3);
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                pg.SetPixel(x, y, new Color32(200, 200, 200, 255));

            pg.RemovePixel(0, 0);

            var moduleSnapshot = new ModuleSnapshot("Module", ModuleType.Production)
            {
                localPosition = new Vector3(5f, 3f, 0f),
                localRotation = Quaternion.identity,
                pixelGrid = pg
            };

            var commandSnapshot = new ModuleSnapshot("Command", ModuleType.Command)
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            };

            var shipSnapshot = new ShipSnapshot("TestShip");
            shipSnapshot.modules.Add(moduleSnapshot);
            shipSnapshot.modules.Add(commandSnapshot);

            _service.ApplySnapshot(ship, shipSnapshot);

            Assert.AreEqual(new Vector3(5f, 3f, 0f), testModule.transform.localPosition,
                "localPosition should be restored");
            Assert.IsFalse(pixelatedRb.PixelGrid.IsPixel(new Vector2Int(0, 0)),
                "Pixel (0,0) should be transparent after apply");
            Assert.IsTrue(pixelatedRb.PixelGrid.IsPixel(new Vector2Int(1, 1)),
                "Pixel (1,1) should be set after apply");
            Object.DestroyImmediate(shipGo);
        }
    }
}