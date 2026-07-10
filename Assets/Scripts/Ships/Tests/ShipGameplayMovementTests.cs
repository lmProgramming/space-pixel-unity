using System.Collections;
using NUnit.Framework;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using Ships.Tests.TestHelpers.Proxies;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipGameplayMovementTests : ShipTestBase
    {
        private const float MovementSimulationSeconds = 2f;
        private const float SASSettleSeconds = 5f;
        private const float MinForwardDistance = 1f;
        private const float MinTurnDegrees = 5f;
        private const float SASHeadingOffsetDegrees = 45f;

        [UnityTest]
        public IEnumerator Ship_MovesForward_WhenInstructedToMoveForward()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            var startPosition = ship.GetPosition();
            var forward = ship.GetForwardForTesting();

            ship.ForwardInput = 1f;
            ship.TurnInput = 0f;
            ship.SASEnabled = false;

            yield return Utils.SimulateForSeconds(MovementSimulationSeconds);

            var forwardTravel = Vector2.Dot(ship.GetPosition() - startPosition, forward);
            Assert.That(forwardTravel, Is.GreaterThan(MinForwardDistance),
                () => $"Expected forward travel > {MinForwardDistance}, got {forwardTravel:F2}.");
        }

        [UnityTest]
        public IEnumerator Ship_TurnsRight_WhenInstructedToTurnRight()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            ship.ForwardInput = 0f;
            ship.TurnInput = -1f;
            ship.SASEnabled = false;

            var heading = new HeadingChangeAccumulator(ship);
            yield return Utils.SimulateForSeconds(MovementSimulationSeconds, heading.Sample);

            Assert.That(heading.TotalDegrees, Is.LessThan(-MinTurnDegrees),
                () => $"Expected right turn > {MinTurnDegrees}°, got {heading.TotalDegrees:F2}°.");
        }

        [UnityTest]
        public IEnumerator Ship_TurnsLeft_WhenInstructedToTurnLeft()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            ship.ForwardInput = 0f;
            ship.TurnInput = 1f;
            ship.SASEnabled = false;

            var heading = new HeadingChangeAccumulator(ship);
            yield return Utils.SimulateForSeconds(MovementSimulationSeconds, heading.Sample);

            Assert.That(heading.TotalDegrees, Is.GreaterThan(MinTurnDegrees),
                () => $"Expected left turn > {MinTurnDegrees}°, got {heading.TotalDegrees:F2}°.");
        }

        [UnityTest]
        public IEnumerator Ship_SASReachesWithinOnePercentOfDesiredHeading_AfterFiveSeconds()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            ship.ForwardInput = 0f;
            ship.TurnInput = 0f;
            ship.SASEnabled = true;

            yield return Utils.SimulateForSeconds(SASSettleSeconds);

            var headingBeforeTargetChange = ship.GetHeadingDegreesForTesting();
            var targetHeading = headingBeforeTargetChange + SASHeadingOffsetDegrees;
            ship.SetSASDesiredHeadingForTesting(targetHeading);

            yield return Utils.SimulateForSeconds(SASSettleSeconds);

            var headingError = Mathf.Abs(Mathf.DeltaAngle(ship.GetHeadingDegreesForTesting(), targetHeading));
            var allowedError = Mathf.Abs(SASHeadingOffsetDegrees) * 0.02f;

            Assert.That(headingError, Is.LessThanOrEqualTo(allowedError),
                () =>
                    $"SAS heading error {headingError:F3}° exceeded 1% tolerance ({allowedError:F3}°) for a {SASHeadingOffsetDegrees}° target.");
        }

        private MovableShipTestProxy CreateReadyShip()
        {
            const int modulePixelSize = 5;
            const float moduleSpacing = 5f;
            const float engineMaxThrust = 0.8f;

            var ship = ShipTestBuilder.CreateShip(Container, CreatedObjects, "GameplayTestShip")
                .WithCommand("Command", Vector2.zero, modulePixelSize, modulePixelSize)
                .WithTestPowerModule(new Vector2(0f, moduleSpacing), modulePixelSize, modulePixelSize)
                .WithEngineModule(new Vector2(moduleSpacing, 0f), engineMaxThrust, modulePixelSize, modulePixelSize)
                .WithEngineModule(new Vector2(-moduleSpacing, 0f), engineMaxThrust, modulePixelSize, modulePixelSize)
                .BuildMovableProxy();

            ship.ConfigureAllocatorForTesting(true, 14, 1f, 0.4f, 0.02f);
            return ship;
        }

        private sealed class HeadingChangeAccumulator
        {
            private readonly MovableShipTestProxy _ship;
            private float _previousHeading;

            public HeadingChangeAccumulator(MovableShipTestProxy ship)
            {
                _ship = ship;
                _previousHeading = ship.GetHeadingDegreesForTesting();
            }

            public float TotalDegrees { get; private set; }

            public void Sample()
            {
                var currentHeading = _ship.GetHeadingDegreesForTesting();
                TotalDegrees += Mathf.DeltaAngle(_previousHeading, currentHeading);
                _previousHeading = currentHeading;
            }
        }
    }
}