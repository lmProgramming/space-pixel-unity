using System.Collections;
using Core.Constants;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ZLinq;

namespace E2E
{
    [TestFixture]
    public class E2ETests : E2ETestBase
    {
        [UnityTest]
        [Retry(3)]
        public IEnumerator Test1_OpposingTeamShipsGoAroundWallAndComeCloseToEachOther()
        {
            // arrange

            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var team2 = CreateTeam("Team2", PhysicsLayers.Enemy);

            var ship1 = CreateAIShip("Ship1", team1, new Vector2(-70f, 0f), false);
            var ship2 = CreateAIShip("Ship2", team2, new Vector2(70f, 0f), false);

            CreateObstacleBox(Vector2.zero,
                new Vector2(300f, 300f));

            CreateObstacleWall("MazeWall", new Vector2(0f, -50f), new Vector2(30f, 300f));

            // act

            yield return WaitForLifecycle();

            var initialDistance = Vector2.Distance(ship1.GetPosition(), ship2.GetPosition());
            Assert.That(initialDistance, Is.GreaterThan(40f));

            var finalDistance = float.MaxValue;
            const float expectedDistance = 35f;
            const float totalTime = 25f;
            const int maxIterations = 100;
            const float step = totalTime / maxIterations;

            for (var i = 0; i < maxIterations; i++)
            {
                yield return SimulateForSeconds(step);

                finalDistance = Vector2.Distance(ship1.GetPosition(), ship2.GetPosition());

                Debug.Log(
                    $"[E2E Maze Test] Initial Distance: {initialDistance:F2}, Final Distance: {finalDistance:F2}");

                if (finalDistance < expectedDistance) break;
            }

            // assert

            Assert.That(finalDistance, Is.LessThan(expectedDistance),
                $"Expected ships to navigate around the wall and close the distance, but got {finalDistance:F2}");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test2_OpposingTeamShipsShootAndDestroyPixels()
        {
            // act 

            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var team2 = CreateTeam("Team2", PhysicsLayers.Enemy);

            var ship1 = CreateAIShip("Ship1", team1, Vector2.zero, true, true);
            var ship2 = CreateAIShip("Ship2", team2, new Vector2(25f, -25f), true, true);

            yield return WaitForLifecycle();

            var ship1StartPixels =
                ship1.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var ship2StartPixels =
                ship2.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);

            // act

            yield return SimulateForSeconds(10f);

            // assert

            var ship1FinalPixels =
                ship1 != null
                    ? ship1.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                    : 0;
            var ship2FinalPixels =
                ship2 != null
                    ? ship2.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                    : 0;

            Debug.Log(
                $"[E2E Shootout] Ship 1 pixels: {ship1StartPixels} -> {ship1FinalPixels}, Ship 2 pixels: {ship2StartPixels} -> {ship2FinalPixels}");

            Assert.That(ship1FinalPixels < ship1StartPixels || ship2FinalPixels < ship2StartPixels,
                "Expected some pixels to be destroyed on the ships after shootout.");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test3_ThreeVsOneShootout()
        {
            // arrange

            var teamA = CreateTeam("TeamA", PhysicsLayers.Friendly);
            var teamB = CreateTeam("TeamB", PhysicsLayers.Enemy);

            var shipB = CreateAIShip("ShipB", teamB, Vector2.zero, true, true);

            var shipA1 = CreateAIShip("ShipA1", teamA, new Vector2(25f, 30f), true, true);
            var shipA2 = CreateAIShip("ShipA2", teamA, new Vector2(25f, -40f), true, true);
            var shipA3 = CreateAIShip("ShipA3", teamA, new Vector2(30f, 0f), true, true);

            yield return WaitForLifecycle();

            var startPixelsB = shipB.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsA1 = shipA1.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsA2 = shipA2.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsA3 = shipA3.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsACombined = startPixelsA1 + startPixelsA2 + startPixelsA3;

            // act

            yield return SimulateForSeconds(10f);

            // assert

            var finalPixelsB = shipB != null
                ? shipB.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                : 0;
            var finalPixelsA1 = shipA1 != null
                ? shipA1.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                : 0;
            var finalPixelsA2 = shipA2 != null
                ? shipA2.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                : 0;
            var finalPixelsA3 = shipA3 != null
                ? shipA3.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                : 0;
            var finalPixelsACombined = finalPixelsA1 + finalPixelsA2 + finalPixelsA3;

            var damageB = startPixelsB - finalPixelsB;
            var damageACombined = startPixelsACombined - finalPixelsACombined;

            Debug.Log($"[E2E 3vs1 Shootout] Ship B damage: {damageB}, Team A combined damage: {damageACombined}");

            Assert.That(damageB, Is.GreaterThan(damageACombined),
                $"Expected the single ship to take more damage ({damageB}) than the 3 attacking ships combined ({damageACombined}).");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test4_FastMovingShipLosesPixelsOnCollisionWithAnotherPixelatedRigidbody()
        {
            // arrange

            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);

            var ship = CreateAIShip("CrashingShip", team1, Vector2.zero, false);

            var asteroid = Instantiator.Instantiate(GetAsteroidPrefab(), new Vector2(80f, 0f), Quaternion.identity);
            CreatedObjects.Add(asteroid);

            yield return WaitForLifecycle();

            // Set the ship flying straight at the asteroid at high speed
            var rb = ship.CommandModule.PixelatedRigidbody.Rigidbody;
            rb.linearVelocity = new Vector2(400f, 0f);

            var startPixels = ship.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount);

            // act

            yield return SimulateForSeconds(1f);

            // assert

            var finalPixels = ship != null
                ? ship.AllModules.AsValueEnumerable().Sum(m => m.PixelatedRigidbody.CurrentPixelCount)
                : 0;

            Debug.Log($"[E2E Impact] Starting pixels: {startPixels}, Final pixels: {finalPixels}");

            Assert.That(finalPixels, Is.LessThan(startPixels),
                "Expected the ship to suffer pixel damage due to physical collision with the asteroid.");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test5_TargetPractice_ShipDestroysStationaryEnemyShip()
        {
            // arrange

            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var team2 = CreateTeam("Team2", PhysicsLayers.Enemy);

            CreateAIShip("ShooterShip", team1, Vector2.zero);
            var targetShip = CreateAIShip("TargetShip", team2, new Vector2(20f, 0f), false);
            var targetGo = targetShip.gameObject;

            CreateObstacleBox(Vector2.zero,
                new Vector2(60f, 40f));

            yield return WaitForLifecycle();

            Assert.IsNotNull(targetShip);

            // act

            yield return SimulateForSeconds(15f);

            // assert

            var targetIsDestroyed = targetShip == null || targetGo == null;

            Debug.Log($"[E2E Target Practice] Target is destroyed: {targetIsDestroyed}");

            Assert.That(targetIsDestroyed, Is.True,
                "Expected the stationary target ship to be completely destroyed after 10 seconds of targeted gunfire.");
        }
    }
}