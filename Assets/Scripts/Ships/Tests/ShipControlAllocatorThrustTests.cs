using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using ZLinq;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipControlAllocatorThrustTests : ShipTestBase
    {
        private struct EngineSpec
        {
            public Vector2 LocalPosition;
            public float LocalRotationZ;
            public float MaxThrust;
        }

        [UnityTest]
        public IEnumerator ForwardInput_OneRearEngine_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = CreateShipWithEngines(
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, -5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(1f, 0f, 0f, 0.02f);

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
                    MaxThrust = 10f
                },
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, 5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(-1f, 0f, 0f, 0.02f);

            Assert.That(shipWithEngines.Engines[0].CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator ForwardInput_FiveHighThrustWingEngines_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = CreateShipWithEngines(
                new EngineSpec { LocalPosition = new Vector2(-20f, -45f), LocalRotationZ = 0f, MaxThrust = 3000f },
                new EngineSpec { LocalPosition = new Vector2(-20f, -50f), LocalRotationZ = 0f, MaxThrust = 3000f },
                new EngineSpec { LocalPosition = new Vector2(0f, -48f), LocalRotationZ = 0f, MaxThrust = 3000f },
                new EngineSpec { LocalPosition = new Vector2(20f, -50f), LocalRotationZ = 0f, MaxThrust = 3000f },
                new EngineSpec { LocalPosition = new Vector2(20f, -45f), LocalRotationZ = 0f, MaxThrust = 3000f });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true, 14, 1f,
                0.4f, 0.02f);

            shipWithEngines.Ship.ApplyEngineForcesForTesting(1f, 0f, 0f, 0.02f);

            foreach (var engine in shipWithEngines.Engines)
                Assert.That(engine.CurrentThrustRatioForTesting, Is.GreaterThanOrEqualTo(0.9f));
        }

        [UnityTest]
        public IEnumerator HorizontalInput_OneRearEngine_UsesAtLeastNinetyPercentThrust()
        {
            var shipWithEngines = CreateShipWithEngines(
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, -5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f
                },
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, 5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(0f, 1f, 0f, 0.02f);

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
                    MaxThrust = 10f
                },
                new EngineSpec
                {
                    LocalPosition = new Vector2(0f, 5f),
                    LocalRotationZ = 0f,
                    MaxThrust = 10f
                });

            yield return WaitForLifecycle();

            shipWithEngines.Ship.ConfigureAllocatorForTesting(true);
            shipWithEngines.Ship.ApplyEngineForcesForTesting(-1f, 0f, 0f, 0.02f);

            Assert.That(shipWithEngines.Engines[1].CurrentThrustRatioForTesting, Is.LessThanOrEqualTo(0.1f));
        }

        private (ShipTestProxy Ship, List<Engine> Engines) CreateShipWithEngines(params EngineSpec[] engineSpecs)
        {
            var shipGo = ModuleFactory.CreateGameObject("AllocatorTestShip", CreatedObjects);
            ModuleFactory.CreateCommandModule(shipGo.transform, Vector2.zero, Container, CreatedObjects, 5, 5);

            var engines = engineSpecs.AsValueEnumerable().Select(spec =>
            {
                ModuleFactory.CreateEngineModule(shipGo.transform, spec.LocalPosition, Container, CreatedObjects,
                    spec.MaxThrust, 5, 5, spec.LocalRotationZ);
                var moduleTransform = shipGo.transform.GetChild(shipGo.transform.childCount - 1);
                return moduleTransform.GetComponent<Engine>();
            }).ToList();

            var ship = ModuleFactory.WireShip<ShipTestProxy>(shipGo, Container);

            Container.InjectGameObject(shipGo);

            return (ship, engines);
        }
    }
}