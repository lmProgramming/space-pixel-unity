using System.Collections;
using NUnit.Framework;
using Services;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Resources;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipDestroyAllModulesTests : ShipTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _snapshotService =
                new ShipSnapshotService(Container, null, new TestModuleCatalog(), new TestContentCatalog());
        }

        private ShipSnapshotService _snapshotService;

        [UnityTest]
        public IEnumerator DestroyAllModules_RemovesModulesFromShipHierarchy()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            Assert.That(ship.GetComponentsInChildren<Module>().Length, Is.EqualTo(2));

            ship.DestroyAllModules();
            yield return null;

            Assert.That(ship.GetComponentsInChildren<Module>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator DestroyAllModules_DetachesModulesFromShipTransform()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            var modulesBeforeDestroy = ship.GetComponentsInChildren<Module>();
            ship.DestroyAllModules();

            foreach (var module in modulesBeforeDestroy)
                Assert.IsNull(module.transform.parent, "Modules should be detached before deferred destroy.");

            yield return null;

            foreach (var module in modulesBeforeDestroy)
                Assert.IsTrue(module == null || module.gameObject == null,
                    "Detached module objects should be destroyed after a frame.");
        }

        [UnityTest]
        public IEnumerator DestroyAllModules_ClearsModuleShipReference()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            var engine = (Engine)ship.AllModules[1];
            ship.DestroyAllModules();

            Assert.IsNull(engine.ShipForTesting);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyAllModules_ThenAddCommand_InitializeModules_RebuildsShip()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            ship.DestroyAllModules();
            yield return null;

            ModuleFactory.CreateCommandModule(ship.transform, Vector2.zero, Container, CreatedObjects, 5, 5);
            ship.InitializeModules();

            Assert.IsNotNull(ship.CommandModule);
            Assert.That(ship.AllModules.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DestroyAllModules_OnEmptyShip_DoesNotThrow()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            ship.DestroyAllModules();
            yield return null;

            Assert.DoesNotThrow(ship.DestroyAllModules);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_AfterDestroyAllModules_ReplacesModulesCorrectly()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            var snapshot = _snapshotService.CaptureSnapshot(ship);
            ship.DestroyAllModules();
            yield return null;

            Assert.That(ship.GetComponentsInChildren<Module>(), Is.Empty);

            _snapshotService.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();
            yield return null;

            Assert.That(ship.AllModules.Count, Is.EqualTo(2));
            Assert.IsNotNull(ship.CommandModule);
            Assert.IsInstanceOf<Engine>(ship.AllModules[1]);
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_WithDamagedPixels_PreservesDamageAfterDestroyAllModules()
        {
            var ship = CreateShipWithCommandAndEngine();
            yield return WaitForLifecycle();

            var engine = (Engine)ship.AllModules[1];
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(2, 2));

            var snapshot = _snapshotService.CaptureSnapshot(ship);
            ship.DestroyAllModules();
            yield return null;

            _snapshotService.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();
            yield return null;

            var restoredEngine = (Engine)ship.AllModules[1];
            Assert.IsFalse(restoredEngine.PixelatedRigidbody.IsPixel(new Vector2Int(2, 2)));
        }

        private Ship CreateShipWithCommandAndEngine()
        {
            var shipGo = ModuleFactory.CreateGameObject("Ship", CreatedObjects);
            shipGo.transform.SetParent(TestRoot.transform);
            shipGo.SetActive(false);

            ModuleFactory.CreateCommandModule(shipGo.transform, Vector2.zero, Container, CreatedObjects, 5, 5);
            ModuleFactory.CreateEngineModule(shipGo.transform, new Vector2(5f, 0f), Container, CreatedObjects, 100f,
                5, 5);

            shipGo.AddComponent<ModuleConnectionFactory>();
            shipGo.AddComponent<ResourceManager>();

            var ship = ModuleFactory.WireShip<Ship>(shipGo, Container);
            ship.InitializeModules();

            Container.InjectGameObject(shipGo);

            return ship;
        }
    }
}