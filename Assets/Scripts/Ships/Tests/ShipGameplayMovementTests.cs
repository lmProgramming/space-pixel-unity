using System.Collections;
using NUnit.Framework;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ships.Tests
{
    [TestFixture]
    public class ShipGameplayMovementTests : ShipTestBase
    {
        private const float MovementSimulationSeconds = 2f;
        private const float SasSettleSeconds = 5f;
        private const float MinForwardDistance = 1f;
        private const float MinTurnDegrees = 5f;
        private const float SasHeadingOffsetDegrees = 45f;

        [UnityTest]
        public IEnumerator Ship_MovesForward_WhenInstructedToMoveForward()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            var startPosition = ship.GetPosition();
            var forward = ship.GetForwardForTesting();

            ship.ForwardInput = 1f;
            ship.TurnInput = 0f;
            ship.SasEnabled = false;

            yield return SimulateForSeconds(MovementSimulationSeconds);

            var forwardTravel = Vector2.Dot(ship.GetPosition() - startPosition, forward);
            Assert.That(forwardTravel, Is.GreaterThan(MinForwardDistance),
                () => $"Expected forward travel > {MinForwardDistance}, got {forwardTravel:F2}.");
        }

        [UnityTest]
        public IEnumerator Ship_TurnsRight_WhenInstructedToTurnRight()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            var startHeading = ship.GetHeadingDegreesForTesting();

            ship.ForwardInput = 0f;
            ship.TurnInput = -1f;
            ship.SasEnabled = false;

            yield return SimulateForSeconds(MovementSimulationSeconds);

            var headingDelta = Mathf.DeltaAngle(startHeading, ship.GetHeadingDegreesForTesting());
            Assert.That(headingDelta, Is.LessThan(-MinTurnDegrees),
                () => $"Expected right turn > {MinTurnDegrees}°, got {headingDelta:F2}°.");
        }

        [UnityTest]
        public IEnumerator Ship_TurnsLeft_WhenInstructedToTurnLeft()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            var startHeading = ship.GetHeadingDegreesForTesting();

            ship.ForwardInput = 0f;
            ship.TurnInput = 1f;
            ship.SasEnabled = false;

            yield return SimulateForSeconds(MovementSimulationSeconds);

            var headingDelta = Mathf.DeltaAngle(startHeading, ship.GetHeadingDegreesForTesting());
            Assert.That(headingDelta, Is.GreaterThan(MinTurnDegrees),
                () => $"Expected left turn > {MinTurnDegrees}°, got {headingDelta:F2}°.");
        }

        [UnityTest]
        public IEnumerator Ship_SasReachesWithinOnePercentOfDesiredHeading_AfterFiveSeconds()
        {
            var ship = CreateReadyShip();
            yield return WaitForLifecycle();

            ship.ForwardInput = 0f;
            ship.TurnInput = 0f;
            ship.SasEnabled = true;

            yield return SimulateForSeconds(SasSettleSeconds);

            var headingBeforeTargetChange = ship.GetHeadingDegreesForTesting();
            var targetHeading = headingBeforeTargetChange + SasHeadingOffsetDegrees;
            ship.SetSasDesiredHeadingForTesting(targetHeading);

            yield return SimulateForSeconds(SasSettleSeconds);

            var headingError = Mathf.Abs(Mathf.DeltaAngle(ship.GetHeadingDegreesForTesting(), targetHeading));
            var allowedError = Mathf.Abs(SasHeadingOffsetDegrees) * 0.01f;

            Assert.That(headingError, Is.LessThanOrEqualTo(allowedError),
                () =>
                    $"SAS heading error {headingError:F3}° exceeded 1% tolerance ({allowedError:F3}°) for a {SasHeadingOffsetDegrees}° target.");
        }

        private MovableShipTestProxy CreateReadyShip()
        {
            return SmallMovableShipTestFactory.Create(Container, CreatedObjects);
        }

        private static IEnumerator SimulateForSeconds(float seconds)
        {
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }
    }
}