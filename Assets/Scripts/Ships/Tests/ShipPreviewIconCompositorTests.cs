using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core.Ships;
using Core.Ships.Module;
using NUnit.Framework;
using Pixelation;
using Services;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Mocks;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipPreviewIconCompositorTests : ShipTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _service = new ShipSnapshotService(new TestContentCatalog());
        }

        [TearDown]
        public override void TearDown()
        {
            foreach (var sprite in _spritesToDestroy)
                ShipPreviewIconCompositor.DestroySprite(sprite);

            _spritesToDestroy.Clear();
            base.TearDown();
        }

        private readonly List<Sprite> _spritesToDestroy = new();
        private ShipSnapshotService _service;

        [UnityTest]
        public IEnumerator ComposeFromShip_SingleModule_CreatesExpectedSprite()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command", Vector2.zero, 1, 1)
                .Build(true);

            yield return null;

            SetModuleColor(ship.AllModules[0], new Color(1f, 0, 0, 1f));

            var sprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromShip(ship));

            Assert.IsNotNull(sprite);
            Assert.AreEqual(1, sprite.texture.width);
            Assert.AreEqual(1, sprite.texture.height);
            Assert.AreEqual(new Color(1f, 0, 0, 1f), sprite.texture.GetPixel(0, 0));
        }

        [UnityTest]
        public IEnumerator ComposeFromSnapshot_SingleModule_CreatesExpectedSprite()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command", Vector2.zero, 1, 1)
                .Build(true);

            yield return null;

            SetModuleColor(ship.AllModules[0], new Color(1f, 0, 0, 1f));

            var snapshot = _service.CaptureSnapshot(ship);
            var sprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromSnapshot(snapshot));

            Assert.IsNotNull(sprite);
            Assert.AreEqual(1, sprite.texture.width);
            Assert.AreEqual(1, sprite.texture.height);
            Assert.AreEqual(new Color(1f, 0, 0, 1f), sprite.texture.GetPixel(0, 0));
        }

        [UnityTest]
        public IEnumerator ComposeFromShip_TwoOffsetModules_IncludesBothInBounds()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command", Vector2.zero, 1, 1)
                .WithBasic("Left", new Vector2(-2f, 0f), 1, 1, new ShipResources())
                .WithBasic("Right", new Vector2(2f, 0f), 1, 1, new ShipResources())
                .Build(true);

            yield return null;

            SetModuleColor(ship.AllModules[1], new Color(1f, 0, 0, 1f));
            SetModuleColor(ship.AllModules[2], new Color(0, 1f, 0, 1f));

            var sprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromShip(ship));

            Assert.IsNotNull(sprite);
            Assert.AreEqual(5, sprite.texture.width);
            Assert.AreEqual(1, sprite.texture.height);
            Assert.AreEqual(new Color(1f, 0, 0, 1f), sprite.texture.GetPixel(0, 0));
            Assert.AreEqual(new Color(0, 1f, 0, 1f), sprite.texture.GetPixel(4, 0));
        }

        [UnityTest]
        public IEnumerator ComposeFromShip_RotatedModule_PlacesPixelsInRotatedPositions()
        {
            var colors = ModuleFactory.CreateSolidPixelGrid(2, 1);
            colors[0, 0] = new Color32(255, 0, 0, 255);
            colors[1, 0] = new Color32(0, 255, 0, 255);

            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommandModule(Vector2.zero, 2, 1, 90f, colors)
                .Build(true);

            yield return null;

            var sprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromShip(ship));

            Assert.IsNotNull(sprite);
            Assert.AreEqual(1, sprite.texture.width);
            Assert.AreEqual(2, sprite.texture.height);
            Assert.AreEqual(new Color(1f, 0, 0, 1f), sprite.texture.GetPixel(0, 0));
            Assert.AreEqual(new Color(0, 1f, 0, 1f), sprite.texture.GetPixel(0, 1));
        }

        [UnityTest]
        public IEnumerator ComposeFromShip_OverlappingModules_LaterModuleWins()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command", new Vector2(10f, 0f), 1, 1)
                .WithBasic("First", Vector2.zero, 1, 1, new ShipResources())
                .WithBasic("Second", Vector2.zero, 1, 1, new ShipResources())
                .Build(true);

            yield return null;

            SetModuleColor(ship.AllModules[1], new Color(1f, 0, 0, 1f));
            SetModuleColor(ship.AllModules[2], new Color(0, 0, 1f, 1f));

            var sprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromShip(ship));

            Assert.IsNotNull(sprite);
            Assert.AreEqual(new Color(0, 0, 1f, 1f), sprite.texture.GetPixel(0, 0));
        }

        [UnityTest]
        public IEnumerator ComposeFromShip_AllTransparent_ReturnsNull()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command", Vector2.zero, 2, 2)
                .Build(true);

            yield return null;

            SetModuleColor(ship.AllModules[0], new Color(0, 0, 0, 0));

            var sprite = ShipPreviewIconCompositor.ComposeFromShip(ship);

            Assert.IsNull(sprite);
        }

        [UnityTest]
        public IEnumerator ComposeFromShip_CustomEngine_ExcludesNozzlePixels()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules[1];
            var nozzle = engine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(nozzle);

            SetModuleColor(engine, new Color(1f, 0, 0, 1f));
            SetPixelatedRigidbodyColor(nozzle, new Color(0, 1f, 0, 1f));

            var sprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromShip(ship));

            Assert.IsNotNull(sprite);
            Assert.IsFalse(ContainsColor(sprite.texture, new Color(0, 1f, 0, 1f)));
        }

        [UnityTest]
        public IEnumerator SaveAndLoadPng_RoundTripsPixelData()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command", Vector2.zero, 1, 1)
                .Build(true);

            yield return null;

            SetModuleColor(ship.AllModules[0], new Color(0.1f, 0.2f, 0.3f, 1f));

            var snapshot = _service.CaptureSnapshot(ship);
            var originalSprite = TrackSprite(ShipPreviewIconCompositor.ComposeFromSnapshot(snapshot));
            var pngPath = Path.Combine(
                Application.temporaryCachePath,
                $"ship-preview-icon-test-{Guid.NewGuid():N}.png");

            try
            {
                ShipPreviewIconCompositor.SavePng(originalSprite.texture, pngPath);

                var loadedSprite = TrackSprite(ShipPreviewIconCompositor.LoadSpriteFromPng(pngPath));

                Assert.IsNotNull(loadedSprite);
                Assert.AreEqual(1, loadedSprite.texture.width);
                Assert.AreEqual(1, loadedSprite.texture.height);
                var sprite = loadedSprite.texture.GetPixel(0, 0);
                Assert.IsTrue(Mathf.Abs(sprite.a - 1f) < 0.02f);
                Assert.IsTrue(Mathf.Abs(sprite.r - 0.1f) < 0.02f);
                Assert.IsTrue(Mathf.Abs(sprite.g - 0.2f) < 0.02f);
                Assert.IsTrue(Mathf.Abs(sprite.b - 0.3f) < 0.02f);
            }
            finally
            {
                if (File.Exists(pngPath))
                    File.Delete(pngPath);
            }
        }

        private static void SetModuleColor(IModule module, Color color)
        {
            SetPixelatedRigidbodyColor(module.PixelatedRigidbody as PixelatedRigidbody, color);
        }

        private static void SetPixelatedRigidbodyColor(PixelatedRigidbody rigidbody, Color color)
        {
            var dimensions = rigidbody.Dimensions();
            var colors = ModuleFactory.CreateSolidPixelGrid(
                dimensions.x,
                dimensions.y,
                color.a > 0 ? color : new Color32(0, 0, 0, 0));

            rigidbody.SetTextureFromColors(colors);
        }

        private static bool ContainsColor(Texture2D texture, Color expectedColor)
        {
            for (var y = 0; y < texture.height; y++)
            for (var x = 0; x < texture.width; x++)
                if (texture.GetPixel(x, y) == expectedColor)
                    return true;

            return false;
        }

        private Sprite TrackSprite(Sprite sprite)
        {
            if (sprite != null)
                _spritesToDestroy.Add(sprite);

            return sprite;
        }
    }
}