using System.Collections;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using NUnit.Framework;
using Services;
using Ships.Modules;
using Ships.Systems.Standalone;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Mocks;
using UnityEngine;
using UnityEngine.TestTools;
using Resources = Core.Ships.Resources;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipDesignModeTests : ShipTestBase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            var contentCatalog = new TestContentCatalog();
            contentCatalog.Seed(Container, CreatedObjects);

            var moduleCatalog = new TestModuleCatalog();
            Container.Rebind<IShipModuleCatalog>()
                .FromInstance(moduleCatalog)
                .AsCached();
            SnapshotRestoreServicesFactory.Rebind(Container, CreatedObjects);

            _snapshotService = new ShipSnapshotService(contentCatalog);
        }

        private ShipSnapshotService _snapshotService;

        [UnityTest]
        public IEnumerator DesignMode_Update_DoesNotChangeEnergy()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("Command", Vector2.zero, 10, 5)
                .WithBasic("Generator", new Vector2(10f, 0f), 10, 5, new Resources(10f, 0f, 5, 0f, 0))
                .BuildDesignShip(true);

            yield return WaitForLifecycle();

            ship.ResourceManager.UpdateEnergy();
            var energyAfterTick = ship.ResourceManager.Energy;

            yield return null;
            yield return null;
            yield return null;

            Assert.That(ship.ResourceManager.Energy, Is.EqualTo(energyAfterTick));
        }

        [UnityTest]
        public IEnumerator DesignMode_InitializeModules_DisablesRigidbodySimulation()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("Command", Vector2.zero, 10, 5)
                .WithEngineModule(new Vector2(10, 0f), 100f, 10, 5)
                .BuildDesignShip(true);

            yield return null;

            ship.InitializeModules();
            yield return null;

            foreach (var engine in ship.Engines)
                Assert.That(engine.PixelatedRigidbody.Rigidbody.simulated, Is.False);
        }

        [UnityTest]
        public IEnumerator DesignMode_ReactionWheel_DoesNotApplyTorqueWhenSasEnabled()
        {
            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("Command", Vector2.zero, 5, 5)
                .AddStandaloneModuleSystemToLastModule<ReactionWheelStabilizer>(new ReactionWheelData
                {
                    data = new ReactionWheelSettings { dampingStrength = 50f }
                })
                .BuildDesignShip();

            ship.InitializeModules();
            yield return WaitForLifecycle();

            var commandRigidbody = ship.CommandModule.PixelatedRigidbody.Rigidbody;
            commandRigidbody.angularVelocity = 30f;

            yield return Utils.SimulateForSeconds(2);

            Assert.That(commandRigidbody.angularVelocity, Is.GreaterThan(25f));
        }

        [UnityTest]
        public IEnumerator ApplySnapshot_OnDesignModeShip_RebuildsModulesWithSimulationDisabled()
        {
            var ship = ShipTestFactory.CreateDesignShipWithCommandAndEngine(Container, CreatedObjects,
                TestRoot.transform);
            yield return WaitForLifecycle();

            var snapshot = _snapshotService.CaptureSnapshot(ship);

            ship.DestroyAllModulesSilently();
            yield return null;

            _snapshotService.ApplySnapshot(ship, snapshot);
            ship.InitializeModules();
            yield return null;

            Assert.That(ship.AllModules.Count, Is.EqualTo(2));
            Assert.IsInstanceOf<Engine>(ship.AllModules[1]);
            Assert.That(((Engine)ship.AllModules[1]).PixelatedRigidbody.Rigidbody.simulated, Is.False);
        }
    }
}