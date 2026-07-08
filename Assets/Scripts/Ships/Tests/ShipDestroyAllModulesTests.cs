using System.Collections;
using Core.Services;
using LMPro.External.IsAlive;
using NUnit.Framework;
using Services;
using Ships.Modules;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Mocks;
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

            var contentCatalog = new TestContentCatalog();
            contentCatalog.Seed(Container, CreatedObjects);

            // AsCached because AsSingle can not be Rebind
            var moduleCatalog = new TestModuleCatalog();
            Container.Rebind<IShipModuleCatalog>()
                .FromInstance(moduleCatalog)
                .AsCached();
            Container.Rebind<IModuleRestoreFactory>()
                .FromInstance(new ModuleRestoreFactory(moduleCatalog))
                .AsCached();

            _snapshotService =
                new ShipSnapshotService(contentCatalog);
        }

        private ShipSnapshotService _snapshotService;

        [UnityTest]
        public IEnumerator DestroyAllModules_DestroysShipAndModules()
        {
            var ship = ShipTestFactory.CreateShipWithCommandAndEngine(Container, CreatedObjects, TestRoot.transform);
            yield return WaitForLifecycle();

            Assert.That(ship.GetComponentsInChildren<Module>().Length, Is.EqualTo(2));

            ship.DestroyAllModules();
            yield return null;

            Assert.That(!ship.IsAliveEnabled());

            var modules = Object.FindObjectsByType<Module>();

            Assert.IsEmpty(modules);
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_AfterDestroyAllModulesSilently_ReplacesModulesCorrectly()
        {
            var ship = ShipTestFactory.CreateShipWithCommandAndEngine(Container, CreatedObjects, TestRoot.transform);
            yield return WaitForLifecycle();

            var snapshot = _snapshotService.CaptureSnapshot(ship);
            ship.DestroyAllModulesSilently();
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
        public IEnumerator ApplySnapshot_WithDamagedPixels_PreservesDamageAfterDestroyAllModulesSilently()
        {
            var ship = ShipTestFactory.CreateShipWithCommandAndEngine(Container, CreatedObjects, TestRoot.transform);
            yield return WaitForLifecycle();

            var engine = (Engine)ship.AllModules[1];
            engine.PixelatedRigidbody.RemovePixelAt(new Vector2Int(2, 2));

            var snapshot = _snapshotService.CaptureSnapshot(ship);
            ship.DestroyAllModulesSilently();
            yield return null;

            _snapshotService.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();
            yield return null;

            var restoredEngine = (Engine)ship.AllModules[1];
            Assert.IsFalse(restoredEngine.PixelatedRigidbody.IsPixel(new Vector2Int(2, 2)));
        }
    }
}