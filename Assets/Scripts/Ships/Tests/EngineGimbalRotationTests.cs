using System.Collections;
using NUnit.Framework;
using Ships.Modules;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class EngineGimbalRotationTests : ShipTestBase
    {
        private const float NearFullCircleCurrentAngle = 170f;
        private const float NearFullCircleTargetAngle = -170f;

        [UnityTest]
        public IEnumerator RotateThrusterTowards_175DegreeMaxGimbal_IncreasesAngleWhenShortestPathCrosses180()
        {
            var engine = CreateEngineWithGimbalRange(175f);
            yield return WaitForLifecycle();

            engine.SetCurrentThrusterAngleForTesting(NearFullCircleCurrentAngle);

            engine.RotateThrusterTowards(NearFullCircleTargetAngle, 0.02f);

            Assert.That(engine.CurrentThrusterAngleForTesting, Is.GreaterThan(NearFullCircleCurrentAngle),
                "Engine should rotate forward through 180 instead of decreasing toward -170 the long way.");
        }

        [UnityTest]
        public IEnumerator RotateThrusterTowards_180DegreeMaxGimbal_IncreasesAngleWhenShortestPathCrosses180()
        {
            var engine = CreateEngineWithGimbalRange(180f);
            yield return WaitForLifecycle();

            engine.SetCurrentThrusterAngleForTesting(NearFullCircleCurrentAngle);

            engine.RotateThrusterTowards(NearFullCircleTargetAngle, 0.02f);

            Assert.That(engine.CurrentThrusterAngleForTesting, Is.GreaterThan(NearFullCircleCurrentAngle),
                "Engine with full gimbal should use 360 degree freedom and take the short arc through 180.");
        }

        private Engine CreateEngineWithGimbalRange(float maxGimbalAngle)
        {
            var shipWithEngines = ShipTestBuilder.CreateShip(Container, CreatedObjects, "GimbalRotationTestShip")
                .WithCommand("Command", Vector2.zero, 5, 5)
                .WithEngineModule(new Vector2(0f, -5f), 10f, 5, 5, gimbalRange: maxGimbalAngle)
                .BuildWithEnginesResult();

            var engine = shipWithEngines.Engines[0];
            engine.ConfigureForTesting(10f, maxGimbalAngle, 60f);
            return engine;
        }
    }
}