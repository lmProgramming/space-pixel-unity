using System.Collections;
using Core.Constants;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace E2E
{
    [TestFixture]
    public class E2ETests : E2ETestBase
    {
        [UnityTest]
        [Retry(3)]
        public IEnumerator Test1_OpposingTeamShipsGoAroundWallAndComeCloseToEachOther()
        {
            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var team2 = CreateTeam("Team2", PhysicsLayers.Enemy);

            var ship1 = CreateAIShip("Ship1", team1, new Vector2(-100f, 0f), false, true);
            var ship2 = CreateAIShip("Ship2", team2, new Vector2(100f, 0f), false, true);

            CreateObstacleBox(Vector2.zero, new Vector2(300f, 300f));
            CreateObstacleWall("MazeWall", new Vector2(-50f, -50f), new Vector2(50f, 300f));
            CreateObstacleWall("MazeWall", new Vector2(50f, 50f), new Vector2(50f, 300f));

            MissionService.Setup();

            yield return WaitForLifecycle();

            const float expectedDistance = 60f;
            var initialDistance = Vector2.Distance(ship1.GetPosition(), ship2.GetPosition());
            Assert.That(initialDistance, Is.GreaterThan(expectedDistance));

            var finalDistance = float.MaxValue;
            const float totalTime = 25f;
            const int maxIterations = 100;
            const float step = totalTime / maxIterations;

            for (var i = 0; i < maxIterations; i++)
            {
                yield return SimulateForSeconds(step);

                finalDistance = Vector2.Distance(ship1.GetPosition(), ship2.GetPosition());
                Debug.Log(
                    $"[E2E Maze Test] Initial Distance: {initialDistance:F2}, Final Distance: {finalDistance:F2}");

                if (finalDistance < expectedDistance)
                    break;
            }

            Assert.That(finalDistance, Is.LessThan(expectedDistance),
                $"Expected ships to navigate around the wall and close the distance, but got {finalDistance:F2}");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test2_OpposingTeamShipsShootAndDestroyPixels()
        {
            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var team2 = CreateTeam("Team2", PhysicsLayers.Enemy);

            var ship1 = CreateAIShip("Ship1", team1, Vector2.zero, true, false);
            var ship2 = CreateAIShip("Ship2", team2, new Vector2(0f, 50f), true, false);

            MissionService.Setup();

            yield return WaitForLifecycle();

            var ship1StartPixels = CountPixels(ship1);
            var ship2StartPixels = CountPixels(ship2);

            yield return SimulateForSeconds(10f);

            var ship1FinalPixels = CountPixels(ship1);
            var ship2FinalPixels = CountPixels(ship2);

            Debug.Log(
                $"[E2E Shootout] Ship 1 pixels: {ship1StartPixels} -> {ship1FinalPixels}, Ship 2 pixels: {ship2StartPixels} -> {ship2FinalPixels}");

            Assert.That(ship1FinalPixels < ship1StartPixels || ship2FinalPixels < ship2StartPixels,
                "Expected some pixels to be destroyed on the ships after shootout.");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test3_ThreeVsOneShootout()
        {
            var teamA = CreateTeam("TeamA", PhysicsLayers.Friendly);
            var teamB = CreateTeam("TeamB", PhysicsLayers.Enemy);

            var shipB = CreateAIShip("ShipB", teamB, Vector2.zero, true, false);
            var shipA1 = CreateAIShip("ShipA1", teamA, new Vector2(25f, 30f), true, false);
            var shipA2 = CreateAIShip("ShipA2", teamA, new Vector2(25f, -40f), true, false);
            var shipA3 = CreateAIShip("ShipA3", teamA, new Vector2(30f, 0f), true, false);

            MissionService.Setup();

            yield return WaitForLifecycle();

            var startPixelsB = CountPixels(shipB);
            var startPixelsACombined = CountPixels(shipA1) + CountPixels(shipA2) + CountPixels(shipA3);

            yield return SimulateForSeconds(10f);

            var finalPixelsB = CountPixels(shipB);
            var finalPixelsACombined = CountPixels(shipA1) + CountPixels(shipA2) + CountPixels(shipA3);

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
            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var ship = CreateAIShip("CrashingShip", team1, Vector2.zero, false, false);

            var asteroid = Instantiator.Instantiate(GetAsteroidPrefab(), new Vector2(80f, 0f), Quaternion.identity);
            CreatedObjects.Add(asteroid);

            MissionService.Setup();

            yield return WaitForLifecycle();

            var rb = ship.CommandModule.PixelatedRigidbody.Rigidbody;
            rb.linearVelocity = new Vector2(1000f, 0f);

            var startPixels = CountPixels(ship);

            yield return SimulateForSeconds(1f);

            var finalPixels = CountPixels(ship);

            Debug.Log($"[E2E Impact] Starting pixels: {startPixels}, Final pixels: {finalPixels}");

            Assert.That(finalPixels, Is.LessThan(startPixels),
                "Expected the ship to suffer pixel damage due to physical collision with the asteroid.");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test5_TargetPractice_ShipDestroysStationaryEnemyShip()
        {
            var team1 = CreateTeam("Team1", PhysicsLayers.Friendly);
            var team2 = CreateTeam("Team2", PhysicsLayers.Enemy);

            CreateAIShip("ShooterShip", team1, Vector2.zero, true, false);
            var targetShip = CreateAIShip("TargetShip", team2, new Vector2(40f, 0f), false, false);
            var targetGo = targetShip.gameObject;

            CreateObstacleBox(Vector2.zero, new Vector2(100f, 80f));

            MissionService.Setup();

            yield return WaitForLifecycle();

            Assert.IsNotNull(targetShip);

            const float testTime = 5f;
            yield return SimulateForSeconds(testTime);

            var targetIsDestroyed = targetShip == null || targetGo == null;

            Debug.Log($"[E2E Target Practice] Target is destroyed: {targetIsDestroyed}");

            Assert.That(targetIsDestroyed, Is.True,
                $"Expected the stationary target ship to be completely destroyed after {testTime} seconds of targeted gunfire.");
        }

        [UnityTest]
        [Retry(3)]
        public IEnumerator Test6_AiShipDealsSignificantDamageToPlayerShip()
        {
            var friendlyTeam = CreateTeam("Friendly", PhysicsLayers.Friendly);
            var enemyTeam = CreateTeam("Enemy", PhysicsLayers.Enemy);

            var player = CreatePlayerShip("Player", friendlyTeam, Vector2.zero, false, false);
            CreateAIShip("Attacker", enemyTeam, new Vector2(0f, 50f), true, false);

            MissionService.Setup();

            yield return WaitForLifecycle();

            var startPixels = CountPixels(player);
            Assert.That(startPixels, Is.GreaterThan(0));

            yield return SimulateForSeconds(10f);

            var finalPixels = CountPixels(player);
            var damage = startPixels - finalPixels;
            const float minDamageRatio = 0.25f;
            var minDamage = Mathf.CeilToInt(startPixels * minDamageRatio);

            Debug.Log(
                $"[E2E Player Damage] Player pixels: {startPixels} -> {finalPixels} (damage {damage}, min {minDamage})");

            Assert.That(damage, Is.GreaterThanOrEqualTo(minDamage),
                $"Expected AI ship to deal significant damage to PlayerShip (>= {minDamageRatio:P0} of pixels).");
        }
    }
}