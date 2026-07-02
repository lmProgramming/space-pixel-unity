using System.Collections;
using Core.Ship;
using Core.Ship.ModuleSnapshotPayloads;
using NUnit.Framework;
using Services;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Mocks;
using UnityEngine;
using UnityEngine.TestTools;
using Resources = Core.Ship.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipSnapshotServiceTests : ShipTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _contentCatalog = new TestContentCatalog();
            _moduleCatalog = new TestModuleCatalog();
            _service = new ShipSnapshotService(Container, null, _moduleCatalog, _contentCatalog);
        }

        private TestContentCatalog _contentCatalog;
        private TestModuleCatalog _moduleCatalog;
        private ShipSnapshotService _service;

        [UnityTest]
        public IEnumerator PrefabOriginCannon_RoundTrip_PreservesProjectileAndSprite()
        {
            var projectilePrefab = new GameObject("ProjectilePrefab");
            var weaponSprite = CreateTestSprite();
            projectilePrefab.transform.SetParent(TestRoot.transform);
            _contentCatalog.AddPrefab("bullet_big", projectilePrefab);
            _contentCatalog.AddSprite("sprite_cannon", weaponSprite);

            var cannonPrefab = ShipTestBuilder.CreateCannonPrefab(Container, CreatedObjects, TestRoot.transform,
                projectilePrefab, weaponSprite);
            _moduleCatalog.Add("cannon_small_16", cannonPrefab);

            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommand(Vector2.zero, 5, 5)
                .WithInstantiatedCannon(cannonPrefab, new Vector2(2f, 0f), "cannon_small_16", 5)
                .Build(true);

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = _service.ToJson(snapshot);
            var fromJson = ShipSnapshotService.FromJson(json);
            _service.ApplySnapshot(ship, fromJson);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var restoredCannon = ((Component)ship.AllModules[1]).GetComponent<Cannon>();
            Assert.IsNotNull(restoredCannon.GetSprite(), "Cannon sprite should be restored from content catalog.");
        }

        [UnityTest]
        public IEnumerator CustomScratchModule_RoundTrip_PreservesColorGrid()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommand(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new Resources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var moduleSnapshot = snapshot.modules[1];
            moduleSnapshot.origin = ModuleOrigin.Custom;
            moduleSnapshot.archetypeId = string.Empty;
            moduleSnapshot.pixelatedRigidbody.colorGrid.RemovePixel(0, 0);

            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var restoredEngine = (Engine)ship.AllModules[1];
            var rb = restoredEngine.PixelatedRigidbody;
            Assert.IsFalse(rb.IsPixel(new Vector2Int(0, 0)));
            Assert.IsTrue(rb.IsPixel(new Vector2Int(1, 1)));
        }

        [UnityTest]
        public IEnumerator PostDamage_RoundTrip_KeepsDestroyedPixelsGone()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommand(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new Resources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules[1];
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(2, 2));
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(3, 3));

            var snapshot = _service.CaptureSnapshot(ship);
            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var restored = (Engine)ship.AllModules[1];
            Assert.IsFalse(restored.PixelatedRigidbody.IsPixel(new Vector2Int(2, 2)));
            Assert.IsFalse(restored.PixelatedRigidbody.IsPixel(new Vector2Int(3, 3)));
        }

        [UnityTest]
        public IEnumerator CustomEngine_Capture_IncludesNozzleSnapshots()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommand(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new Resources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var engineSnapshot = snapshot.modules[1];
            var engineData = JsonUtility.FromJson<EngineModuleData>(engineSnapshot.typePayloadJson);

            yield return null;
            yield return null;

            Assert.That(engineData.nozzles, Is.Not.Null);
            Assert.That(engineData.nozzles.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(engineData.nozzles[0].rigidbodyType, Is.EqualTo(PixelatedRigidbodyType.Nozzle));
        }

        [UnityTest]
        public IEnumerator CustomEngine_NozzleDamage_RoundTrip_PreservesDestroyedPixels()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommand(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new Resources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules[1];
            var nozzle = engine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(nozzle);
            nozzle.RemovePixelAt(new Vector2Int(1, 1));

            var snapshot = _service.CaptureSnapshot(ship);
            snapshot.modules[1].origin = ModuleOrigin.Custom;
            snapshot.modules[1].archetypeId = string.Empty;

            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var restoredEngine = (Engine)ship.AllModules[1];
            var restoredNozzle = restoredEngine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(restoredNozzle);
            Assert.IsFalse(restoredNozzle.IsPixel(new Vector2Int(1, 1)));
            Assert.IsTrue(restoredNozzle.IsPixel(new Vector2Int(0, 0)));
        }

        [UnityTest]
        public IEnumerator ShipWithAllModules_HasAllModulesAfterSnapshotRestoration()
        {
            //todo: finish this!
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCustomCommand(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(5f, 0f), 5, 5, new Resources(0, 2f, 0, 0, 0))
                //.WithTestPowerModule(new Vector2(0f, 0f), 5, 5)
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules[1];
            var nozzle = engine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(nozzle);
            nozzle.RemovePixelAt(new Vector2Int(1, 1));

            var snapshot = _service.CaptureSnapshot(ship);
            snapshot.modules[1].origin = ModuleOrigin.Custom;
            snapshot.modules[1].archetypeId = string.Empty;

            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var restoredEngine = (Engine)ship.AllModules[1];
            var restoredNozzle = restoredEngine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(restoredNozzle);
            Assert.IsFalse(restoredNozzle.IsPixel(new Vector2Int(1, 1)));
            Assert.IsTrue(restoredNozzle.IsPixel(new Vector2Int(0, 0)));
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }
    }
}