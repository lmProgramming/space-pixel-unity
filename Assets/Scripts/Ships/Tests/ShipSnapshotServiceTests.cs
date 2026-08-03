using System.Collections;
using Core.Pixelation;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.ModuleData;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using NUnit.Framework;
using Services;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Systems.Standalone;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Mocks;
using UnityEngine;
using UnityEngine.TestTools;
using ZLinq;

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
            _contentCatalog.Seed(Container, CreatedObjects);
            _moduleCatalog = new TestModuleCatalog();

            // AsCached because AsSingle can not be Rebind
            Container.Rebind<IShipModuleCatalog>()
                .FromInstance(_moduleCatalog)
                .AsCached();
            SnapshotRestoreServicesFactory.Rebind(Container, CreatedObjects);

            _service = new ShipSnapshotService(_contentCatalog);
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
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithInstantiatedModule("cannon", cannonPrefab, new Vector2(2f, 0f), "cannon_small_16", 5)
                .Build(true);

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var json = JsonUtility.ToJson(snapshot, true);
            var fromJson = JsonUtility.FromJson<ShipSnapshot>(json);
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
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var moduleSnapshot = snapshot.modules[1];
            moduleSnapshot.origin = InstanceOrigin.Custom;
            moduleSnapshot.archetypeId = string.Empty;
            moduleSnapshot.pixelatedRigidbody.colorGrid.RemovePixelAt(new Vector2Int(0, 0));

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
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
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
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
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
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules[1];
            var nozzle = engine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(nozzle);
            nozzle.RemovePixelAt(new Vector2Int(1, 1));

            var snapshot = _service.CaptureSnapshot(ship);
            snapshot.modules[1].origin = InstanceOrigin.Custom;
            snapshot.modules[1].archetypeId = string.Empty;

            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var restoredEngine = (Engine)ship.AllModules[1];
            var restoredNozzle = restoredEngine.GetComponentInChildren<Nozzle>();
            Assert.IsNotNull(restoredNozzle);
            Assert.IsNotNull(((IPixelatedRigidbody)restoredNozzle).CollisionHandler);
            Assert.IsFalse(restoredNozzle.IsPixel(new Vector2Int(1, 1)));
            Assert.IsTrue(restoredNozzle.IsPixel(new Vector2Int(0, 0)));
        }

        [UnityTest]
        public IEnumerator ShipWithAllModules_HasAllModulesAfterSnapshotRestoration()
        {
            var (projectilePrefab, weaponSprite) = CreateSimpleCannonDependencies();
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommand("Command Module", Vector2.zero, 5, 5)
                .AddStandaloneModuleSystemToLastModule<ReactionWheelStabilizer>(new ReactionWheelData
                {
                    data = new ReactionWheelSettings
                    {
                        dampingStrength = 1234
                    }
                })
                .WithCustomEngine(new Vector2(5f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
                .WithBasic("Power Module", new Vector2(0f, 5f), 15, 5, new ShipResources(100, 5, 1, 500, 0))
                .WithBasic("Crew Module", new Vector2(0f, 10f), 5, 5, new ShipResources(100, 25, 1, 0, 10))
                .WithBasic("Battery", new Vector2(-5f, 10f), 5, 5, new ShipResources(5000, 10, 1, 0, 0))
                .WithLaser("Laser", new Vector2(5f, 5f), 5, 5)
                .WithCannon(projectilePrefab, weaponSprite, new Vector2(10f, 5f), 5, 5)
                .Build(true);

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            _service.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();

            yield return null;
            yield return null;

            var allModules = ship.AllModules;
            var allModulesEnumerable = ship.AllModules.AsValueEnumerable();
            Assert.IsTrue(allModules.Count == 7);
            Assert.IsTrue(allModulesEnumerable.Where(m => m.Type == ModuleType.Command).Count() == 1);

            var commandModule = ship.CommandModule;
            var reactionWheel = commandModule.Transform?.gameObject.GetComponentInChildren<ReactionWheelStabilizer>();
            Assert.IsNotNull(reactionWheel);

            Assert.AreEqual(reactionWheel.GetSettingsForTesting().dampingStrength, 1234);

            Assert.IsTrue(allModulesEnumerable.Where(m => m.Type == ModuleType.Engine).Count() == 1);
            Assert.IsTrue(allModulesEnumerable.Where(m => m.Type == ModuleType.Resources).Count() == 3);
            Assert.IsTrue(allModulesEnumerable.Where(m => m.Type == ModuleType.Weapon).Count() == 2);
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_IncludesBlueprintAndStartPixelCount()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "Ship")
                .ParentedTo(TestRoot.transform)
                .WithCommandOfCustomSnapshotOrigin(Vector2.zero, 5, 5)
                .WithCustomEngine(new Vector2(2f, 0f), 5, 5, new ShipResources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules[1];
            var startCount = engine.PixelatedRigidbody.StartPixelCount;
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(2, 2));

            var snapshot = _service.CaptureSnapshot(ship);
            Assert.That(snapshot.blueprint, Is.Not.Null);
            Assert.That(snapshot.blueprint.modules.Count, Is.EqualTo(2));
            Assert.That(snapshot.modules[1].pixelatedRigidbody.startPixelCount, Is.EqualTo(startCount));

            var json = JsonUtility.ToJson(snapshot, true);
            var fromJson = JsonUtility.FromJson<ShipSnapshot>(json);
            fromJson.blueprint.modules[1].removedByPlayer = true;

            _service.ApplySnapshot(ship, fromJson);
            ship.InitializeModules();

            yield return null;

            Assert.That(ship.Blueprint.modules.Count, Is.EqualTo(2));
            Assert.That(ship.Blueprint.modules[1].removedByPlayer, Is.True);
            Assert.That(ship.AllModules[1].PixelatedRigidbody.StartPixelCount, Is.EqualTo(startCount));
        }

        [UnityTest]
        public IEnumerator CaptureSnapshot_AfterWorldMovement_PreservesCommandRelativeLayout()
        {
            var engineLayout = new Vector2(5f, 0f);
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "LayoutShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithCustomEngine(engineLayout, 5, 5, new ShipResources(0, 2f, 0, 0, 0))
                .Build(true);

            yield return null;

            var engine = (Engine)ship.AllModules.AsValueEnumerable().First(m => m.Type == ModuleType.Engine);
            var expectedLayout = ShipLayoutSpace.WorldToLocal(ship, engine.Transform!.position);

            var worldDelta = new Vector3(37f, -12f, 0f);
            foreach (var module in ship.AllModules)
                module.Transform!.position += worldDelta;

            yield return null;

            var snapshot = _service.CaptureSnapshot(ship);
            var commandSnapshot = snapshot.modules.AsValueEnumerable()
                .First(m => m.concreteModuleType == ConcreteModuleType.Command);
            var engineSnapshot = snapshot.modules.AsValueEnumerable()
                .First(m => m.concreteModuleType == ConcreteModuleType.Engine);

            Assert.That(commandSnapshot.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(engineSnapshot.localPosition.x, Is.EqualTo(expectedLayout.x).Within(0.01f));
            Assert.That(engineSnapshot.localPosition.y, Is.EqualTo(expectedLayout.y).Within(0.01f));
        }

        private (GameObject projectilePrefab, Sprite weaponSprite) CreateSimpleCannonDependencies()
        {
            var projectilePrefab = new GameObject("ProjectilePrefab");
            var weaponSprite = CreateTestSprite();
            projectilePrefab.transform.SetParent(TestRoot.transform);

            return (projectilePrefab, weaponSprite);
        }

        private static Sprite CreateTestSprite()
        {
            var texture = new Texture2D(2, 2);
            return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        }
    }
}