using System.Collections;
using NUnit.Framework;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipControlAllocatorThrustTests : ShipTestBase
    {
        [UnityTest]
        public IEnumerator ForwardInput_OneRearEngine_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = ShipTestBuilder.CreateShip(Container, CreatedObjects, "AllocatorTestShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithEngineModule(new Vector2(0f, -5f), 10f, 5, 5)
                .BuildWithEnginesResult();

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(1f, 0f, 0f, 0.02f);

            yield return Utils.SimulateForSeconds(1f);

            Assert.That(shipWithEngines.Engines[0].CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator BackwardInput_BackwardFacingEngine_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = ShipTestBuilder.CreateShip(Container, CreatedObjects, "AllocatorTestShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithEngineModule(new Vector2(0f, -5f), 1f, 5, 5, 180f)
                .WithEngineModule(new Vector2(0f, 5f), 1f, 5, 5)
                .BuildWithEnginesResult();

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(-1f, 0f, 0f, 0.02f);

            yield return Utils.SimulateForSeconds(1f);

            Assert.That(shipWithEngines.Engines[0].CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator ForwardInput_FiveHighThrustWingEngines_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = ShipTestBuilder.CreateShip(Container, CreatedObjects, "AllocatorTestShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithEngineModule(new Vector2(-5f, 0f), 3000f, 5, 5)
                .WithEngineModule(new Vector2(-10f, 0f), 3000f, 5, 5)
                .WithEngineModule(new Vector2(0f, -5f), 3000f, 5, 5)
                .WithEngineModule(new Vector2(5f, 0f), 3000f, 5, 5)
                .WithEngineModule(new Vector2(10f, 0f), 3000f, 5, 5)
                .BuildWithEnginesResult();

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true, 14, 1f,
                0.4f, 0.02f);

            shipWithEngines.Ship.ApplyEngineForcesForTesting(1f, 0f, 0f, 0.02f);

            yield return Utils.SimulateForSeconds(1f);

            foreach (var engine in shipWithEngines.Engines)
                Assert.That(engine.CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator HorizontalInput_TwoSymmetricEngines_EngineUsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = ShipTestBuilder.CreateShip(Container, CreatedObjects, "AllocatorTestShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithEngineModule(new Vector2(0f, -5f), 1f, 5, 5)
                .WithEngineModule(new Vector2(0f, 5f), 1f, 5, 5)
                .BuildWithEnginesResult();

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(0f, 1f, 0f, 0.02f);

            yield return Utils.SimulateForSeconds(1f);

            Assert.That(shipWithEngines.Engines[0].CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator BackwardInput_ForwardFacingEngine_UsesAtMostTenPercentThrust()
        {
            var shipWithEngines = ShipTestBuilder.CreateShip(Container, CreatedObjects, "AllocatorTestShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithEngineModule(new Vector2(0f, -5f), 1f, 5, 5, 180f)
                .WithEngineModule(new Vector2(0f, 5f), 1f, 5, 5)
                .BuildWithEnginesResult();

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(-1f, 0f, 0f, 0.02f);

            yield return Utils.SimulateForSeconds(1f);

            Assert.That(shipWithEngines.Engines[1].CurrentThrustRatioForTesting, Is.LessThanOrEqualTo(0.1f));
        }
    }
}