using System;
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
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipSnapshotServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root");
            _container = TestContainerFactory.CreateTestContainer(_root.transform);
            _service = new ShipSnapshotService(_container);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        private ShipSnapshotService _service;
        private DiContainer _container;
        private GameObject _root;

        /// <summary>
        ///     Creates a minimal ship with a Command module and optional extra modules.
        ///     All modules are built from raw Color32 arrays — no sprite prefabs.
        /// </summary>
        private Ship CreateShipWithModules(
            Color32[,] commandColors,
            params (string name, ModuleType type, Type componentType, Color32[,] colors, Vector3 pos)[] extras)
        {
            var shipGo = new GameObject("Ship");
            shipGo.transform.SetParent(_root.transform);
            shipGo.SetActive(false);

            // Command module
            var cmdGo = CreateModuleGameObject("Command", commandColors, shipGo.transform, Vector3.zero);
            cmdGo.AddComponent<Command>();

            foreach (var (name, type, componentType, colors, pos) in extras)
            {
                var go = CreateModuleGameObject(name, colors, shipGo.transform, pos);

                if (componentType == typeof(TestModule))
                {
                    var tm = go.AddComponent<TestModule>();
                    tm.SetModuleType(type);
                }
                else
                {
                    go.AddComponent(componentType);
                }
            }

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();
            var ship = shipGo.AddComponent<Ship>();
            _container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;

            shipGo.SetActive(true);
            return ship;
        }

        private GameObject CreateModuleGameObject(string name, Color32[,] colors, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.AddComponent<SpriteRenderer>();
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            go.AddComponent<PolygonCollider2D>();
            var pxRb = go.AddComponent<PixelatedRigidbody>();
            _container.Inject(pxRb);
            pxRb.SetTextureFromColors(colors);
            return go;
        }

        private static Color32[,] MakeSolidGrid(int w, int h, Color32 color)
        {
            var grid = new Color32[w, h];
            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                grid[x, y] = color;
            return grid;
        }

        private static Color32[,] MakeCheckerboardGrid(int w, int h, Color32 a, Color32 b)
        {
            var grid = new Color32[w, h];
            for (var x = 0; x < w; x++)
            for (var y = 0; y < h; y++)
                grid[x, y] = (x + y) % 2 == 0 ? a : b;
            return grid;
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_WhenModuleHasSprite_SnapshotColorsTakePriorityOverSprite()
        {
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
            _container.Inject(pixelatedRb);

            var spriteTexture = new Texture2D(3, 3, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point };
            var spriteColors = new Color32[9];
            for (var i = 0; i < 9; i++)
                spriteColors[i] = new Color32(255, 0, 0, 255);
            spriteTexture.SetPixels32(spriteColors);
            spriteTexture.Apply();
            var testSprite = Sprite.Create(spriteTexture, new Rect(0, 0, 3, 3), new Vector2(0.5f, 0.5f), 1);

            pixelatedRb.SetSpriteForTesting(testSprite);
            pixelatedRb.Setup(null, true);

            var testModule = moduleGo.AddComponent<TestModule>();
            testModule.SetModuleType(ModuleType.Resources);

            var commandGo = new GameObject("Command");
            commandGo.transform.SetParent(shipGo.transform);
            commandGo.AddComponent<SpriteRenderer>();
            var cmdRb2d = commandGo.AddComponent<Rigidbody2D>();
            cmdRb2d.bodyType = RigidbodyType2D.Dynamic;
            cmdRb2d.gravityScale = 0f;
            commandGo.AddComponent<PolygonCollider2D>();
            var commandPixelatedRb = commandGo.AddComponent<PixelatedRigidbody>();
            _container.Inject(commandPixelatedRb);
            var commandColors = new Color32[3, 3];
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                commandColors[x, y] = new Color32(50, 50, 50, 255);
            commandPixelatedRb.SetTextureFromColors(commandColors);
            commandGo.AddComponent<Command>();

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();
            var ship = shipGo.AddComponent<Ship>();
            _container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;

            shipGo.SetActive(true);
            yield return null;

            var pg = new PixelGridSnapshot(3, 3);
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                pg.SetPixel(x, y, new Color32(0, 0, 255, 255));
            pg.RemovePixel(2, 2);

            var moduleSnapshot = new ModuleSnapshot("Module", ModuleType.Resources, nameof(Module))
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                pixelGrid = pg
            };
            var commandPg = new PixelGridSnapshot(3, 3);
            for (var cx = 0; cx < 3; cx++)
            for (var cy = 0; cy < 3; cy++)
                commandPg.SetPixel(cx, cy, new Color32(50, 50, 50, 255));

            var commandSnapshot = new ModuleSnapshot("Command", ModuleType.Command, nameof(Command))
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                pixelGrid = commandPg
            };

            var shipSnapshot = new ShipSnapshot("TestShip");
            shipSnapshot.modules.Add(moduleSnapshot);
            shipSnapshot.modules.Add(commandSnapshot);

            _service.ApplySnapshot(ship, shipSnapshot);

            // Re-query module references since ApplySnapshot destroys and recreates them
            var newModules = ship.GetComponentsInChildren<Module>();
            var newModule = newModules.AsValueEnumerable().FirstOrDefault(m => m is not Command);

            Assert.IsNotNull(newModule, "Should find a non-command module after apply");
            var newPixelatedRb = newModule.PixelatedRigidbody;

            var pixel = newPixelatedRb.PixelGrid.GetValue(new Vector2Int(0, 0));
            Assert.AreEqual(0, pixel.r,
                "Red channel should be 0 (snapshot blue), not 255 (sprite red) — snapshot must override sprite");
            Assert.AreEqual(255, pixel.b,
                "Blue channel should be 255 from snapshot color, not overridden by sprite");
            Assert.IsFalse(newPixelatedRb.PixelGrid.IsPixel(new Vector2Int(2, 2)),
                "Pixel (2,2) should be transparent as set in snapshot");

            Object.DestroyImmediate(shipGo);
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_ThenApplySnapshot_RoundTrip_PreservesPixelColors_WithSprite()
        {
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
            _container.Inject(pixelatedRb);

            var spriteTexture = new Texture2D(4, 4, TextureFormat.ARGB32, false) { filterMode = FilterMode.Point };
            var spriteColors = new Color32[16];
            for (var i = 0; i < 16; i++)
                spriteColors[i] = new Color32(255, 0, 0, 255);
            spriteTexture.SetPixels32(spriteColors);
            spriteTexture.Apply();
            var testSprite = Sprite.Create(spriteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1);
            pixelatedRb.SetSpriteForTesting(testSprite);
            pixelatedRb.Setup(null, true);

            var testModule = moduleGo.AddComponent<TestModule>();
            testModule.SetModuleType(ModuleType.Resources);

            var commandGo = new GameObject("Command");
            commandGo.transform.SetParent(shipGo.transform);
            commandGo.AddComponent<SpriteRenderer>();
            var cmdRb2d = commandGo.AddComponent<Rigidbody2D>();
            cmdRb2d.bodyType = RigidbodyType2D.Dynamic;
            cmdRb2d.gravityScale = 0f;
            commandGo.AddComponent<PolygonCollider2D>();
            var commandPixelatedRb = commandGo.AddComponent<PixelatedRigidbody>();
            _container.Inject(commandPixelatedRb);
            var commandColors = new Color32[4, 4];
            for (var x = 0; x < 4; x++)
            for (var y = 0; y < 4; y++)
                commandColors[x, y] = new Color32(50, 50, 50, 255);
            commandPixelatedRb.SetTextureFromColors(commandColors);
            commandGo.AddComponent<Command>();

            var connectionFactory = shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();
            var ship = shipGo.AddComponent<Ship>();
            _container.Inject(ship);
            ship.ModuleConnectionFactoryForTesting = connectionFactory;

            shipGo.SetActive(true);
            yield return null;

            pixelatedRb.RemovePixelAt(new Vector2Int(0, 0));
            pixelatedRb.RemovePixelAt(new Vector2Int(3, 3));

            var snapshot = _service.CaptureSnapshot(ship);

            _service.ApplySnapshot(ship, snapshot);

            // Re-query module references since ApplySnapshot destroys and recreates them
            var newModules = ship.GetComponentsInChildren<Module>();
            var newPixelatedRb = (
                from m in newModules.AsValueEnumerable()
                where m is not Command
                select (PixelatedRigidbody)m.PixelatedRigidbody
            ).FirstOrDefault();

            Assert.IsNotNull(newPixelatedRb, "Should find a non-command module's PixelatedRigidbody after apply");

            Assert.IsFalse(newPixelatedRb.PixelGrid.IsPixel(new Vector2Int(0, 0)),
                "Pixel (0,0) was removed before capture — must stay removed after round-trip, not be restored by sprite");
            Assert.IsFalse(newPixelatedRb.PixelGrid.IsPixel(new Vector2Int(3, 3)),
                "Pixel (3,3) was removed before capture — must stay removed after round-trip, not be restored by sprite");
            Assert.IsTrue(newPixelatedRb.PixelGrid.IsPixel(new Vector2Int(1, 1)),
                "Pixel (1,1) was intact — must still be set after round-trip");

            Object.DestroyImmediate(shipGo);
        }

        // ───────────────────────────────────────────────────────────────
        // Pure-data tests (no MonoBehaviours, no coroutine needed)
        // ───────────────────────────────────────────────────────────────

        [Test]
        public void PixelGridSnapshot_SetAndGet_PreservesExactColors()
        {
            var pg = new PixelGridSnapshot(3, 2);
            var red = new Color32(255, 0, 0, 255);
            var green = new Color32(0, 255, 0, 128);
            var blue = new Color32(0, 0, 255, 255);
            var transparent = new Color32(0, 0, 0, 0);

            pg.SetPixel(0, 0, red);
            pg.SetPixel(1, 0, green);
            pg.SetPixel(2, 0, blue);
            pg.SetPixel(0, 1, transparent);
            pg.SetPixel(1, 1, new Color32(42, 99, 200, 77));
            pg.SetPixel(2, 1, new Color32(255, 255, 255, 255));

            Assert.AreEqual(red, pg.GetPixel(0, 0));
            Assert.AreEqual(green, pg.GetPixel(1, 0));
            Assert.AreEqual(blue, pg.GetPixel(2, 0));
            Assert.AreEqual(transparent, pg.GetPixel(0, 1));
            Assert.AreEqual(77, pg.GetPixel(1, 1).a);
            Assert.AreEqual(255, pg.GetPixel(2, 1).r);
        }

        [Test]
        public void PixelGridSnapshot_IsPixel_ReturnsFalseForTransparent()
        {
            var pg = new PixelGridSnapshot(2, 2);
            pg.SetPixel(0, 0, new Color32(255, 0, 0, 255));
            pg.SetPixel(1, 0, new Color32(0, 0, 0, 0));
            pg.SetPixel(0, 1, new Color32(0, 0, 0, 1));
            pg.SetPixel(1, 1, new Color32(0, 0, 0, 0));

            Assert.IsTrue(pg.IsPixel(0, 0));
            Assert.IsFalse(pg.IsPixel(1, 0));
            Assert.IsTrue(pg.IsPixel(0, 1), "Alpha=1 should count as pixel");
            Assert.IsFalse(pg.IsPixel(1, 1));
        }

        [Test]
        public void PixelGridSnapshot_RemovePixel_ClearsToTransparent()
        {
            var pg = new PixelGridSnapshot(2, 2);
            pg.SetPixel(0, 0, new Color32(100, 100, 100, 255));
            pg.SetPixel(1, 1, new Color32(200, 200, 200, 255));

            pg.RemovePixel(0, 0);

            Assert.IsFalse(pg.IsPixel(0, 0));
            Assert.IsTrue(pg.IsPixel(1, 1));
        }

        [Test]
        public void PixelGridSnapshot_GetAllNonTransparentPixelPositions_ReturnsCorrectSet()
        {
            var pg = new PixelGridSnapshot(3, 3);
            pg.SetPixel(0, 0, new Color32(255, 0, 0, 255));
            pg.SetPixel(2, 2, new Color32(0, 255, 0, 255));
            // rest stays default (transparent)

            var positions = pg.GetAllNonTransparentPixelPositions();

            Assert.AreEqual(2, positions.Count);
            Assert.Contains(new Vector2Int(0, 0), positions);
            Assert.Contains(new Vector2Int(2, 2), positions);
        }

        [Test]
        public void PixelGridSnapshot_JsonRoundTrip_PreservesEveryPixelColor()
        {
            var pg = new PixelGridSnapshot(4, 3);
            var colors = new Color32[]
            {
                new(255, 0, 0, 255), new(0, 255, 0, 255), new(0, 0, 255, 255), new(0, 0, 0, 0),
                new(128, 64, 32, 200), new(1, 2, 3, 4), new(255, 255, 255, 255), new(0, 0, 0, 1),
                new(10, 20, 30, 40), new(50, 60, 70, 80), new(90, 100, 110, 120), new(130, 140, 150, 160)
            };
            for (var i = 0; i < colors.Length; i++)
                pg.SetPixel(i % 4, i / 4, colors[i]);

            var json = JsonUtility.ToJson(pg);
            var restored = JsonUtility.FromJson<PixelGridSnapshot>(json);

            Assert.AreEqual(pg.width, restored.width);
            Assert.AreEqual(pg.height, restored.height);

            for (var y = 0; y < pg.height; y++)
            for (var x = 0; x < pg.width; x++)
            {
                var expected = pg.GetPixel(x, y);
                var actual = restored.GetPixel(x, y);
                Assert.AreEqual(expected, actual,
                    $"Pixel ({x},{y}) mismatch: expected {expected}, got {actual}");
            }
        }

        [Test]
        public void ModuleSnapshot_JsonRoundTrip_PreservesAllFields()
        {
            var ms = new ModuleSnapshot("TestEngine", ModuleType.Engine, nameof(Engine))
            {
                localPosition = new Vector3(1.5f, -2.3f, 0f),
                localRotation = Quaternion.Euler(0, 0, 45),
                resources = new Resources(100f, 10f, 5, 50f, 10),
                pixelGrid = new PixelGridSnapshot(2, 2)
            };
            ms.pixelGrid.SetPixel(0, 0, new Color32(255, 0, 0, 255));
            ms.pixelGrid.SetPixel(1, 0, new Color32(0, 255, 0, 255));
            ms.pixelGrid.SetPixel(0, 1, new Color32(0, 0, 255, 255));
            ms.pixelGrid.SetPixel(1, 1, new Color32(0, 0, 0, 0));

            var json = JsonUtility.ToJson(ms);
            var restored = JsonUtility.FromJson<ModuleSnapshot>(json);

            Assert.AreEqual("TestEngine", restored.moduleName);
            Assert.AreEqual(ModuleType.Engine, restored.moduleType);
            Assert.AreEqual(nameof(Engine), restored.moduleTypeName);
            Assert.AreEqual(1.5f, restored.localPosition.x, 0.001f);
            Assert.AreEqual(-2.3f, restored.localPosition.y, 0.001f);
            Assert.AreEqual(100f, restored.resources.energyCapacity, 0.001f);
            Assert.AreEqual(10f, restored.resources.energyDraw, 0.001f);
            Assert.AreEqual(50f, restored.resources.energyProduction, 0.001f);
            Assert.AreEqual(5, restored.resources.crewNeeded);
            Assert.AreEqual(10, restored.resources.crewQuarters);
            Assert.AreEqual(new Color32(255, 0, 0, 255), restored.pixelGrid.GetPixel(0, 0));
            Assert.AreEqual(new Color32(0, 255, 0, 255), restored.pixelGrid.GetPixel(1, 0));
            Assert.IsFalse(restored.pixelGrid.IsPixel(1, 1));
        }

        [Test]
        public void ShipSnapshot_JsonRoundTrip_PreservesModulesAndConnections()
        {
            var snapshot = new ShipSnapshot("MyShip");
            snapshot.commandModuleIndex = 0;

            var cmd = new ModuleSnapshot("Cmd", ModuleType.Command, nameof(Command))
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                resources = new Resources(0, 0, 2, 0, 4),
                pixelGrid = new PixelGridSnapshot(2, 2)
            };
            cmd.pixelGrid.SetPixel(0, 0, new Color32(50, 50, 50, 255));
            cmd.pixelGrid.SetPixel(1, 0, new Color32(50, 50, 50, 255));
            cmd.pixelGrid.SetPixel(0, 1, new Color32(50, 50, 50, 255));
            cmd.pixelGrid.SetPixel(1, 1, new Color32(50, 50, 50, 255));
            snapshot.modules.Add(cmd);

            var cannon = new ModuleSnapshot("Gun", ModuleType.Weapon, nameof(Cannon))
            {
                localPosition = new Vector3(3f, 0f, 0f),
                localRotation = Quaternion.identity,
                resources = new Resources(0, 5f, 1, 0, 0),
                pixelGrid = new PixelGridSnapshot(2, 1)
            };
            cannon.pixelGrid.SetPixel(0, 0, new Color32(200, 100, 50, 255));
            cannon.pixelGrid.SetPixel(1, 0, new Color32(200, 100, 50, 255));
            snapshot.modules.Add(cannon);

            var conn = new ModuleConnection(0, 1);
            conn.connectionPointsA.Add(new Vector2Int(1, 0));
            conn.connectionPointsB.Add(new Vector2Int(0, 0));
            snapshot.connections.Add(conn);

            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            Assert.AreEqual("MyShip", restored.shipName);
            Assert.AreEqual(0, restored.commandModuleIndex);
            Assert.AreEqual(2, restored.modules.Count);
            Assert.AreEqual(1, restored.connections.Count);

            Assert.AreEqual("Cmd", restored.modules[0].moduleName);
            Assert.AreEqual(ModuleType.Command, restored.modules[0].moduleType);
            Assert.AreEqual(nameof(Command), restored.modules[0].moduleTypeName);
            Assert.AreEqual(new Color32(50, 50, 50, 255), restored.modules[0].pixelGrid.GetPixel(0, 0));

            Assert.AreEqual("Gun", restored.modules[1].moduleName);
            Assert.AreEqual(ModuleType.Weapon, restored.modules[1].moduleType);
            Assert.AreEqual(3f, restored.modules[1].localPosition.x, 0.001f);
            Assert.AreEqual(new Color32(200, 100, 50, 255), restored.modules[1].pixelGrid.GetPixel(0, 0));

            var restoredConn = restored.connections[0];
            Assert.AreEqual(0, restoredConn.moduleIndexA);
            Assert.AreEqual(1, restoredConn.moduleIndexB);
            Assert.AreEqual(1, restoredConn.connectionPointsA.Count);
            Assert.AreEqual(new Vector2Int(1, 0), restoredConn.connectionPointsA[0]);
        }

        [Test]
        public void FromJson_HardcodedTinyShip_DeserializesExactPixelColors()
        {
            // Hand-crafted JSON for a 2×2 command module with specific pixel colors
            const string json = @"{
                ""shipName"": ""TinyShip"",
                ""modules"": [
                    {
                        ""moduleName"": ""Cmd"",
                        ""moduleType"": 0,
                        ""moduleTypeName"": ""Command"",
                        ""localPosition"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0 },
                        ""localRotation"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0, ""w"": 1.0 },
                        ""pixelGrid"": {
                            ""width"": 2,
                            ""height"": 2,
                            ""pixels"": [
                                { ""r"": 255, ""g"": 0,   ""b"": 0,   ""a"": 255 },
                                { ""r"": 0,   ""g"": 255, ""b"": 0,   ""a"": 255 },
                                { ""r"": 0,   ""g"": 0,   ""b"": 255, ""a"": 255 },
                                { ""r"": 0,   ""g"": 0,   ""b"": 0,   ""a"": 0   }
                            ]
                        },
                        ""resources"": {
                            ""energyCapacity"": 0.0,
                            ""energyDraw"": 0.0,
                            ""energyProduction"": 0.0,
                            ""crewNeeded"": 0,
                            ""crewQuarters"": 0
                        },
                        ""moduleComponentJson"": """"
                    }
                ],
                ""connections"": [],
                ""commandModuleIndex"": 0
            }";

            var snapshot = _service.FromJson(json);

            Assert.AreEqual("TinyShip", snapshot.shipName);
            Assert.AreEqual(1, snapshot.modules.Count);

            var pg = snapshot.modules[0].pixelGrid;
            Assert.AreEqual(2, pg.width);
            Assert.AreEqual(2, pg.height);

            // Row-major: index = y * width + x
            // (0,0) = index 0 = red
            Assert.AreEqual(new Color32(255, 0, 0, 255), pg.GetPixel(0, 0), "Pixel (0,0) should be red");
            // (1,0) = index 1 = green
            Assert.AreEqual(new Color32(0, 255, 0, 255), pg.GetPixel(1, 0), "Pixel (1,0) should be green");
            // (0,1) = index 2 = blue
            Assert.AreEqual(new Color32(0, 0, 255, 255), pg.GetPixel(0, 1), "Pixel (0,1) should be blue");
            // (1,1) = index 3 = transparent
            Assert.IsFalse(pg.IsPixel(1, 1), "Pixel (1,1) should be transparent");
        }

        [Test]
        public void FromJson_TwoModuleShip_DeserializesPositionsAndResources()
        {
            const string json = @"{
                ""shipName"": ""DualShip"",
                ""modules"": [
                    {
                        ""moduleName"": ""Bridge"",
                        ""moduleType"": 0,
                        ""moduleTypeName"": ""Command"",
                        ""localPosition"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0 },
                        ""localRotation"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0, ""w"": 1.0 },
                        ""pixelGrid"": {
                            ""width"": 1,
                            ""height"": 1,
                            ""pixels"": [ { ""r"": 80, ""g"": 80, ""b"": 80, ""a"": 255 } ]
                        },
                        ""resources"": {
                            ""energyCapacity"": 100.0,
                            ""energyDraw"": 0.0,
                            ""energyProduction"": 20.0,
                            ""crewNeeded"": 3,
                            ""crewQuarters"": 5
                        },
                        ""moduleComponentJson"": """"
                    },
                    {
                        ""moduleName"": ""Thruster"",
                        ""moduleType"": 3,
                        ""moduleTypeName"": ""Engine"",
                        ""localPosition"": { ""x"": -5.5, ""y"": 2.25, ""z"": 0.0 },
                        ""localRotation"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.3826834, ""w"": 0.9238795 },
                        ""pixelGrid"": {
                            ""width"": 2,
                            ""height"": 1,
                            ""pixels"": [
                                { ""r"": 120, ""g"": 60, ""b"": 30, ""a"": 255 },
                                { ""r"": 121, ""g"": 61, ""b"": 31, ""a"": 200 }
                            ]
                        },
                        ""resources"": {
                            ""energyCapacity"": 0.0,
                            ""energyDraw"": 15.0,
                            ""energyProduction"": 0.0,
                            ""crewNeeded"": 1,
                            ""crewQuarters"": 2
                        },
                        ""moduleComponentJson"": """"
                    }
                ],
                ""connections"": [
                    {
                        ""moduleIndexA"": 0,
                        ""moduleIndexB"": 1,
                        ""connectionPointsA"": [ { ""x"": 0, ""y"": 0 } ],
                        ""connectionPointsB"": [ { ""x"": 0, ""y"": 0 } ]
                    }
                ],
                ""commandModuleIndex"": 0
            }";

            var snapshot = _service.FromJson(json);

            Assert.AreEqual("DualShip", snapshot.shipName);
            Assert.AreEqual(2, snapshot.modules.Count);
            Assert.AreEqual(0, snapshot.commandModuleIndex);

            // Bridge
            var bridge = snapshot.modules[0];
            Assert.AreEqual("Bridge", bridge.moduleName);
            Assert.AreEqual(ModuleType.Command, bridge.moduleType);
            Assert.AreEqual(100f, bridge.resources.energyCapacity, 0.001f);
            Assert.AreEqual(20f, bridge.resources.energyProduction, 0.001f);
            Assert.AreEqual(3, bridge.resources.crewNeeded);
            Assert.AreEqual(new Color32(80, 80, 80, 255), bridge.pixelGrid.GetPixel(0, 0));

            // Thruster
            var thruster = snapshot.modules[1];
            Assert.AreEqual("Thruster", thruster.moduleName);
            Assert.AreEqual(ModuleType.Engine, thruster.moduleType);
            Assert.AreEqual(-5.5f, thruster.localPosition.x, 0.001f);
            Assert.AreEqual(2.25f, thruster.localPosition.y, 0.001f);
            Assert.AreEqual(0.3826834f, thruster.localRotation.z, 0.001f);
            Assert.AreEqual(15f, thruster.resources.energyDraw, 0.001f);
            Assert.AreEqual(new Color32(120, 60, 30, 255), thruster.pixelGrid.GetPixel(0, 0));
            Assert.AreEqual(200, thruster.pixelGrid.GetPixel(1, 0).a,
                "Alpha should be exactly 200, not clamped to 255");

            // Connection
            Assert.AreEqual(1, snapshot.connections.Count);
            Assert.AreEqual(0, snapshot.connections[0].moduleIndexA);
            Assert.AreEqual(1, snapshot.connections[0].moduleIndexB);
        }

        [Test]
        public void ToJson_ThenFromJson_IdenticalSnapshot()
        {
            var original = new ShipSnapshot("RoundTripper");
            var ms = new ModuleSnapshot("OnlyModule", ModuleType.Command, nameof(Command))
            {
                localPosition = new Vector3(7.7f, -3.3f, 0),
                localRotation = Quaternion.Euler(0, 0, 90),
                resources = new Resources(50, 25, 3, 10, 6),
                pixelGrid = new PixelGridSnapshot(3, 3)
            };
            // Unique color per pixel
            for (var y = 0; y < 3; y++)
            for (var x = 0; x < 3; x++)
                ms.pixelGrid.SetPixel(x, y, new Color32((byte)(x * 80), (byte)(y * 80), (byte)((x + y) * 40), 255));

            ms.pixelGrid.RemovePixel(1, 1); // center pixel transparent
            original.modules.Add(ms);
            original.commandModuleIndex = 0;

            var json = _service.ToJson(original);
            var restored = _service.FromJson(json);

            Assert.AreEqual(original.shipName, restored.shipName);
            Assert.AreEqual(original.commandModuleIndex, restored.commandModuleIndex);
            Assert.AreEqual(original.modules.Count, restored.modules.Count);

            var oPg = original.modules[0].pixelGrid;
            var rPg = restored.modules[0].pixelGrid;
            Assert.AreEqual(oPg.width, rPg.width);
            Assert.AreEqual(oPg.height, rPg.height);

            for (var y = 0; y < oPg.height; y++)
            for (var x = 0; x < oPg.width; x++)
                Assert.AreEqual(oPg.GetPixel(x, y), rPg.GetPixel(x, y),
                    $"Pixel ({x},{y}) differs after JSON round-trip");
        }

        [Test]
        public void FromJson_MultipleModuleTypes_TypeNamesPreserved()
        {
            var snapshot = new ShipSnapshot("Arsenal");
            snapshot.modules.Add(new ModuleSnapshot("Cmd", ModuleType.Command, nameof(Command))
            {
                pixelGrid = new PixelGridSnapshot(1, 1)
            });
            snapshot.modules[0].pixelGrid.SetPixel(0, 0, new Color32(1, 1, 1, 255));

            snapshot.modules.Add(new ModuleSnapshot("Gun", ModuleType.Weapon, nameof(Cannon))
            {
                pixelGrid = new PixelGridSnapshot(1, 1)
            });
            snapshot.modules[1].pixelGrid.SetPixel(0, 0, new Color32(2, 2, 2, 255));

            snapshot.modules.Add(new ModuleSnapshot("Beam", ModuleType.Weapon, nameof(LaserBeam))
            {
                pixelGrid = new PixelGridSnapshot(1, 1)
            });
            snapshot.modules[2].pixelGrid.SetPixel(0, 0, new Color32(3, 3, 3, 255));

            snapshot.modules.Add(new ModuleSnapshot("Thruster", ModuleType.Engine, nameof(Engine))
            {
                pixelGrid = new PixelGridSnapshot(1, 1)
            });
            snapshot.modules[3].pixelGrid.SetPixel(0, 0, new Color32(4, 4, 4, 255));

            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            Assert.AreEqual(4, restored.modules.Count);
            Assert.AreEqual(nameof(Command), restored.modules[0].moduleTypeName);
            Assert.AreEqual(nameof(Cannon), restored.modules[1].moduleTypeName);
            Assert.AreEqual(nameof(LaserBeam), restored.modules[2].moduleTypeName);
            Assert.AreEqual(nameof(Engine), restored.modules[3].moduleTypeName);
        }

        // ───────────────────────────────────────────────────────────────
        // Play-mode tests — full capture → JSON → apply cycle
        // ───────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator CaptureSnapshot_2x2Command_AllPixelColorsSurviveJsonRoundTrip()
        {
            var colors = new Color32[2, 2];
            colors[0, 0] = new Color32(255, 0, 0, 255);
            colors[1, 0] = new Color32(0, 255, 0, 255);
            colors[0, 1] = new Color32(0, 0, 255, 255);
            colors[1, 1] = new Color32(0, 0, 0, 0); // transparent

            var ship = CreateShipWithModules(colors);
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            var pg = restored.modules[snapshot.commandModuleIndex].pixelGrid;
            Assert.AreEqual(2, pg.width);
            Assert.AreEqual(2, pg.height);
            Assert.AreEqual(new Color32(255, 0, 0, 255), pg.GetPixel(0, 0));
            Assert.AreEqual(new Color32(0, 255, 0, 255), pg.GetPixel(1, 0));
            Assert.AreEqual(new Color32(0, 0, 255, 255), pg.GetPixel(0, 1));
            Assert.IsFalse(pg.IsPixel(1, 1));
        }

        [UnityTest]
        public IEnumerator CaptureAndApply_3x3Checkerboard_PixelPatternSurvives()
        {
            var red = new Color32(200, 50, 50, 255);
            var blue = new Color32(50, 50, 200, 255);
            var cmdColors = MakeCheckerboardGrid(3, 3, red, blue);

            var ship = CreateShipWithModules(cmdColors);
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var fromJson = _service.FromJson(json);

            _service.ApplySnapshot(ship, fromJson);

            var cmdModule = ship.GetComponentInChildren<Command>();
            Assert.IsNotNull(cmdModule);
            var grid = cmdModule.PixelatedRigidbody.PixelGrid;

            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
            {
                var expected = (x + y) % 2 == 0 ? red : blue;
                var actual = grid.GetValue(new Vector2Int(x, y));
                Assert.AreEqual(expected.r, actual.r, $"Pixel ({x},{y}) red channel mismatch");
                Assert.AreEqual(expected.g, actual.g, $"Pixel ({x},{y}) green channel mismatch");
                Assert.AreEqual(expected.b, actual.b, $"Pixel ({x},{y}) blue channel mismatch");
                Assert.AreEqual(expected.a, actual.a, $"Pixel ({x},{y}) alpha channel mismatch");
            }
        }

        [UnityTest]
        public IEnumerator CaptureAndApply_WithRemovedPixels_RemovedPixelsStayRemoved()
        {
            var cmdColors = MakeSolidGrid(4, 4, new Color32(100, 100, 100, 255));
            var ship = CreateShipWithModules(cmdColors);
            yield return null;

            // Remove some pixels before capturing
            var cmdPxRb = (PixelatedRigidbody)ship.GetComponentInChildren<Command>().PixelatedRigidbody;
            Assert.IsNotNull(cmdPxRb, "Command PixelatedRigidbody should exist");
            cmdPxRb.RemovePixelAt(new Vector2Int(0, 0));
            cmdPxRb.RemovePixelAt(new Vector2Int(3, 3));
            cmdPxRb.RemovePixelAt(new Vector2Int(1, 2));

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var fromJson = _service.FromJson(json);
            _service.ApplySnapshot(ship, fromJson);

            var newCmd = ship.GetComponentInChildren<Command>();
            var grid = newCmd.PixelatedRigidbody.PixelGrid;

            Assert.IsFalse(grid.IsPixel(new Vector2Int(0, 0)), "(0,0) was removed, must stay removed");
            Assert.IsFalse(grid.IsPixel(new Vector2Int(3, 3)), "(3,3) was removed, must stay removed");
            Assert.IsFalse(grid.IsPixel(new Vector2Int(1, 2)), "(1,2) was removed, must stay removed");
            Assert.IsTrue(grid.IsPixel(new Vector2Int(1, 1)), "(1,1) was intact, must still be set");
            Assert.IsTrue(grid.IsPixel(new Vector2Int(2, 2)), "(2,2) was intact, must still be set");
        }

        [UnityTest]
        public IEnumerator CaptureAndApply_MultiModuleShip_AllModulesPreserved()
        {
            var cmdColors = MakeSolidGrid(3, 3, new Color32(50, 50, 50, 255));
            var engineColors = MakeSolidGrid(2, 4, new Color32(0, 200, 0, 255));
            var cannonColors = MakeSolidGrid(2, 2, new Color32(200, 0, 0, 255));

            var ship = CreateShipWithModules(cmdColors,
                ("Engine1", ModuleType.Engine, typeof(TestModule), engineColors, new Vector3(-5, 0, 0)),
                ("Cannon1", ModuleType.Weapon, typeof(TestModule), cannonColors, new Vector3(5, 0, 0))
            );
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            Assert.AreEqual(3, snapshot.modules.Count, "Should capture 3 modules");

            var json = _service.ToJson(snapshot);
            var fromJson = _service.FromJson(json);

            Assert.AreEqual(3, fromJson.modules.Count, "JSON round-trip should preserve 3 modules");

            // Verify each module's pixel data survived
            foreach (var ms in fromJson.modules)
            {
                Assert.IsNotNull(ms.pixelGrid, $"Module '{ms.moduleName}' has null pixelGrid after JSON");
                Assert.IsTrue(ms.pixelGrid.width > 0, $"Module '{ms.moduleName}' pixelGrid width should be > 0");

                var nonTransparent = ms.pixelGrid.GetAllNonTransparentPixelPositions();
                Assert.IsTrue(nonTransparent.Count > 0,
                    $"Module '{ms.moduleName}' should have at least one non-transparent pixel");
            }
        }

        [UnityTest]
        public IEnumerator ApplyFromJson_HardcodedTinyShip_CreatesCorrectPixels()
        {
            // Start with a ship that has different pixels
            var originalColors = MakeSolidGrid(2, 2, new Color32(99, 99, 99, 255));
            var ship = CreateShipWithModules(originalColors);
            yield return null;

            // Apply a hardcoded JSON with completely different colors
            const string json = @"{
                ""shipName"": ""Overwrite"",
                ""modules"": [
                    {
                        ""moduleName"": ""Cmd"",
                        ""moduleType"": 0,
                        ""moduleTypeName"": ""Command"",
                        ""localPosition"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0 },
                        ""localRotation"": { ""x"": 0.0, ""y"": 0.0, ""z"": 0.0, ""w"": 1.0 },
                        ""pixelGrid"": {
                            ""width"": 2,
                            ""height"": 2,
                            ""pixels"": [
                                { ""r"": 10, ""g"": 20, ""b"": 30, ""a"": 255 },
                                { ""r"": 40, ""g"": 50, ""b"": 60, ""a"": 255 },
                                { ""r"": 70, ""g"": 80, ""b"": 90, ""a"": 255 },
                                { ""r"": 0,  ""g"": 0,  ""b"": 0,  ""a"": 0   }
                            ]
                        },
                        ""resources"": {
                            ""energyCapacity"": 0.0,
                            ""energyDraw"": 0.0,
                            ""energyProduction"": 0.0,
                            ""crewNeeded"": 0,
                            ""crewQuarters"": 0
                        },
                        ""moduleComponentJson"": """"
                    }
                ],
                ""connections"": [],
                ""commandModuleIndex"": 0
            }";

            var snapshot = _service.FromJson(json);
            _service.ApplySnapshot(ship, snapshot);

            var cmd = ship.GetComponentInChildren<Command>();
            Assert.IsNotNull(cmd, "Command module should exist after apply");

            var grid = cmd.PixelatedRigidbody.PixelGrid;
            var c00 = grid.GetValue(new Vector2Int(0, 0));
            Assert.AreEqual(10, c00.r, "Pixel (0,0) R should be 10 from JSON, not 99 from original");
            Assert.AreEqual(20, c00.g);
            Assert.AreEqual(30, c00.b);

            var c10 = grid.GetValue(new Vector2Int(1, 0));
            Assert.AreEqual(40, c10.r);
            Assert.AreEqual(50, c10.g);
            Assert.AreEqual(60, c10.b);

            var c01 = grid.GetValue(new Vector2Int(0, 1));
            Assert.AreEqual(70, c01.r);
            Assert.AreEqual(80, c01.g);
            Assert.AreEqual(90, c01.b);

            Assert.IsFalse(grid.IsPixel(new Vector2Int(1, 1)),
                "(1,1) should be transparent as defined in JSON");
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_ModulePosition_PreservedAfterJsonRoundTrip()
        {
            var cmdColors = MakeSolidGrid(2, 2, new Color32(50, 50, 50, 255));
            var extraColors = MakeSolidGrid(2, 2, new Color32(100, 100, 100, 255));
            var expectedPos = new Vector3(7.5f, -3.2f, 0f);

            var ship = CreateShipWithModules(cmdColors,
                ("OffsetModule", ModuleType.Resources, typeof(TestModule), extraColors, expectedPos)
            );
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            var offsetMs =
                restored.modules.AsValueEnumerable().FirstOrDefault(ms => ms.moduleName == "OffsetModule");

            Assert.IsNotNull(offsetMs, "OffsetModule should exist in restored snapshot");
            Assert.AreEqual(expectedPos.x, offsetMs.localPosition.x, 0.01f, "X position mismatch");
            Assert.AreEqual(expectedPos.y, offsetMs.localPosition.y, 0.01f, "Y position mismatch");
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_UniqueColorPerPixel_AllColorsPreservedExactly()
        {
            // 4×4 command module where every pixel has a unique color
            var cmdColors = new Color32[4, 4];
            for (var x = 0; x < 4; x++)
            for (var y = 0; y < 4; y++)
                cmdColors[x, y] = new Color32(
                    (byte)(x * 60 + 10),
                    (byte)(y * 60 + 10),
                    (byte)((x + y) * 30 + 5),
                    255);

            var ship = CreateShipWithModules(cmdColors);
            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            _service.ApplySnapshot(ship, restored);

            var grid = ship.GetComponentInChildren<Command>().PixelatedRigidbody.PixelGrid;

            for (var x = 0; x < 4; x++)
            for (var y = 0; y < 4; y++)
            {
                var expected = cmdColors[x, y];
                var actual = grid.GetValue(new Vector2Int(x, y));
                Assert.AreEqual(expected.r, actual.r, $"({x},{y}) R mismatch");
                Assert.AreEqual(expected.g, actual.g, $"({x},{y}) G mismatch");
                Assert.AreEqual(expected.b, actual.b, $"({x},{y}) B mismatch");
                Assert.AreEqual(expected.a, actual.a, $"({x},{y}) A mismatch");
            }
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_ReplacesAllExistingModules()
        {
            // Ship starts with Command + 2 extras
            var cmdColors = MakeSolidGrid(2, 2, new Color32(50, 50, 50, 255));
            var extraA = MakeSolidGrid(2, 2, new Color32(100, 0, 0, 255));
            var extraB = MakeSolidGrid(2, 2, new Color32(0, 100, 0, 255));

            var ship = CreateShipWithModules(cmdColors,
                ("ModA", ModuleType.Resources, typeof(TestModule), extraA, new Vector3(-3, 0, 0)),
                ("ModB", ModuleType.Resources, typeof(TestModule), extraB, new Vector3(3, 0, 0))
            );
            yield return null;

            var moduleCountBefore = ship.GetComponentsInChildren<Module>().Length;
            Assert.AreEqual(3, moduleCountBefore, "Should start with 3 modules");

            // Apply a snapshot with only 1 module (just Command)
            var snapshot = new ShipSnapshot("SlimShip");
            var cmdMs = new ModuleSnapshot("Cmd", ModuleType.Command, nameof(Command))
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                pixelGrid = new PixelGridSnapshot(2, 2)
            };
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
                cmdMs.pixelGrid.SetPixel(x, y, new Color32(80, 80, 80, 255));

            snapshot.modules.Add(cmdMs);
            snapshot.commandModuleIndex = 0;

            _service.ApplySnapshot(ship, snapshot);

            var modulesAfter = ship.GetComponentsInChildren<Module>();
            Assert.AreEqual(1, modulesAfter.Length,
                "After applying a 1-module snapshot, ship should have exactly 1 module");
            Assert.IsNotNull(ship.GetComponentInChildren<Command>(),
                "The single remaining module should be Command");
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_Resources_SurviveJsonRoundTrip()
        {
            var cmdColors = MakeSolidGrid(2, 2, new Color32(50, 50, 50, 255));
            var ship = CreateShipWithModules(cmdColors);
            yield return null;

            Module cmdModule = ship.GetComponentInChildren<Command>();
            cmdModule.SetResources(new Resources(200f, 50f, 8, 75f, 12));

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var restored = _service.FromJson(json);

            var cmdSnap = restored.modules[restored.commandModuleIndex];
            Assert.AreEqual(200f, cmdSnap.resources.energyCapacity, 0.001f);
            Assert.AreEqual(50f, cmdSnap.resources.energyDraw, 0.001f);
            Assert.AreEqual(75f, cmdSnap.resources.energyProduction, 0.001f);
            Assert.AreEqual(8, cmdSnap.resources.crewNeeded);
            Assert.AreEqual(12, cmdSnap.resources.crewQuarters);
        }
    }
}