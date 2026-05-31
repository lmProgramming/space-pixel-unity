using System.Collections;
using System.Linq;
using NUnit.Framework;
using Ships;
using Ships.Internal;
using Ships.Tests.TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;

namespace E2E
{
    [TestFixture]
    public class E2ETests : E2ETestBase
    {
        [UnityTest]
        public IEnumerator Test1_NavigateEachOtherInAsteroidMaze()
        {
            var team1 = CreateTeam("Team1");
            var team2 = CreateTeam("Team2");

            // Spawn ships on opposite teams: Ship 1 at (0, 0), Ship 2 at (45, 0)
            var ship1 = CreateAIShip("Ship1", team1, new Vector2(-45f, 0f), false);
            var ship2 = CreateAIShip("Ship2", team2, new Vector2(45f, 0f), false);

            // Set up a maze of obstacle walls blocking direct navigation at x = 20
            // Sector size is 10. Let's block sectors (20, -20), (20, -10), (20, 0), leaving (20, 10) and above open.
            // We place a large wall from y = -25 to y = 5.
            CreateObstacleWall("MazeWall", new Vector2(0f, -50f), new Vector2(30f, 300f));

            yield return WaitForLifecycle();

            var initialDistance = Vector2.Distance(ship1.GetPosition(), ship2.GetPosition());
            Assert.That(initialDistance, Is.GreaterThan(40f));

            // Simulate the ships navigating around the maze wall for 10 seconds
            yield return SimulateForSeconds(10f);

            var finalDistance = Vector2.Distance(ship1.GetPosition(), ship2.GetPosition());

            Debug.Log($"[E2E Maze Test] Initial Distance: {initialDistance:F2}, Final Distance: {finalDistance:F2}");

            // The ships should have successfully calculated a path around the obstacle and closed the distance.
            Assert.That(finalDistance, Is.LessThan(15f),
                $"Expected ships to navigate around the wall and close the distance, but got {finalDistance:F2}");
        }

        [UnityTest]
        public IEnumerator Test2_ShootAndDestroyPixels()
        {
            var team1 = CreateTeam("Team1");
            var team2 = CreateTeam("Team2");
            var bulletPrefab = CreateBulletPrefab();

            // Spawn ships facing each other at close range
            var ship1 = CreateAIShip("Ship1", team1, Vector2.zero, true, bulletPrefab);
            var ship2 = CreateAIShip("Ship2", team2, new Vector2(25f, 0f), true, bulletPrefab);

            yield return WaitForLifecycle();

            // Store initial active pixel counts for both ships across all their modules
            var ship1StartPixels = ship1.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var ship2StartPixels = ship2.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);

            // Simulate shootout for 10 seconds
            yield return SimulateForSeconds(10f);

            var ship1FinalPixels =
                ship1 != null ? ship1.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;
            var ship2FinalPixels =
                ship2 != null ? ship2.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;

            Debug.Log(
                $"[E2E Shootout] Ship 1 pixels: {ship1StartPixels} -> {ship1FinalPixels}, Ship 2 pixels: {ship2StartPixels} -> {ship2FinalPixels}");

            // Verify that some pixels have been destroyed on at least one of the ships (or both)
            Assert.That(ship1FinalPixels < ship1StartPixels || ship2FinalPixels < ship2StartPixels,
                "Expected some pixels to be destroyed on the ships after a 10 seconds shootout.");
        }

        [UnityTest]
        public IEnumerator Test3_ThreeVsOneShootout()
        {
            var teamA = CreateTeam("TeamA");
            var teamB = CreateTeam("TeamB");
            var bulletPrefab = CreateBulletPrefab();

            // Team B has 1 lonely ship at (0, 0)
            var shipB = CreateAIShip("ShipB", teamB, Vector2.zero, true, bulletPrefab);

            // Team A has 3 ships surrounding Ship B
            var shipA1 = CreateAIShip("ShipA1", teamA, new Vector2(25f, 10f), true, bulletPrefab);
            var shipA2 = CreateAIShip("ShipA2", teamA, new Vector2(25f, -10f), true, bulletPrefab);
            var shipA3 = CreateAIShip("ShipA3", teamA, new Vector2(30f, 0f), true, bulletPrefab);

            yield return WaitForLifecycle();

            var startPixelsB = shipB.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsA1 = shipA1.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsA2 = shipA2.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsA3 = shipA3.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);
            var startPixelsACombined = startPixelsA1 + startPixelsA2 + startPixelsA3;

            yield return SimulateForSeconds(10f);

            var finalPixelsB = shipB != null ? shipB.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;
            var finalPixelsA1 = shipA1 != null ? shipA1.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;
            var finalPixelsA2 = shipA2 != null ? shipA2.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;
            var finalPixelsA3 = shipA3 != null ? shipA3.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;
            var finalPixelsACombined = finalPixelsA1 + finalPixelsA2 + finalPixelsA3;

            var damageB = startPixelsB - finalPixelsB;
            var damageACombined = startPixelsACombined - finalPixelsACombined;

            Debug.Log($"[E2E 3vs1 Shootout] Ship B damage: {damageB}, Team A combined damage: {damageACombined}");

            // The 3 ships of Team A should damage Team B's ship significantly more than they themselves take damage combined.
            Assert.That(damageB, Is.GreaterThan(damageACombined),
                $"Expected the single ship to take more damage ({damageB}) than the 3 attacking ships combined ({damageACombined}).");
        }

        [UnityTest]
        public IEnumerator Test4_AsteroidImpactDamagesShipModule()
        {
            var team1 = CreateTeam("Team1");

            // Spawn a ship at (0, 0)
            var ship = CreateAIShip("CrashingShip", team1, Vector2.zero, false);

            // Spawn a solid, static asteroid wall right in its path at (15, 0)
            CreateObstacleWall("Asteroid", new Vector2(15f, 0f), new Vector2(5f, 15f));

            yield return WaitForLifecycle();

            // Set the ship flying straight at the asteroid at high speed
            var rb = ship.CommandModule.PixelatedRigidbody.Rigidbody;
            rb.linearVelocity = new Vector2(40f, 0f);

            var startPixels = ship.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount);

            // Simulate physics for 2 seconds (enough to impact and calculate collisions)
            yield return SimulateForSeconds(2f);

            var finalPixels = ship != null ? ship.AllModules.Sum(m => m.PixelatedRigidbody.CurrentPixelCount) : 0;

            Debug.Log($"[E2E Impact] Starting pixels: {startPixels}, Final pixels: {finalPixels}");

            // The collision should have resulted in a significant loss of pixels
            Assert.That(finalPixels, Is.LessThan(startPixels),
                "Expected the ship to suffer pixel damage due to physical collision with the asteroid.");
        }

        [UnityTest]
        public IEnumerator Test5_TargetPractice_DestroysStationaryEnemyShip()
        {
            var team1 = CreateTeam("Team1");
            var team2 = CreateTeam("Team2");
            var bulletPrefab = CreateBulletPrefab();

            // Spawn a shooter ship (Team 1) at (0, 0) with weapons
            CreateAIShip("ShooterShip", team1, Vector2.zero, true, bulletPrefab);

            // Spawn a stationary target ship (Team 2) at (18, 0) with NO engines (so it's a sitting duck)
            var targetGo = ModuleFactory.CreateGameObject("StationaryTarget", CreatedObjects);
            targetGo.transform.position = new Vector2(18f, 0f);

            ModuleFactory.CreateCommandModule(targetGo.transform, Vector2.zero, Container, CreatedObjects, 5, 5);
            ModuleFactory.CreatePowerModule(targetGo.transform, new Vector2(0f, 5f), Container, CreatedObjects, 5, 5);

            var connectionFactory = targetGo.AddComponent<ModuleConnectionFactory>();
            targetGo.AddComponent<ResourceManager>();

            targetGo.SetActive(false);
            var targetShip = targetGo.AddComponent<Ship>();
            targetShip.SetTeam(team2);
            Container.Inject(targetShip);
            targetShip.ModuleConnectionFactoryForTesting = connectionFactory;
            targetGo.SetActive(true);

            yield return WaitForLifecycle();

            // Initially, the target ship exists
            Assert.IsNotNull(targetShip);

            // Simulate shootout for 10 seconds - the shooter ship will target and fire continuously at the target
            yield return SimulateForSeconds(10f);

            // The target ship's command module should be completely destroyed, which destroys the ship itself
            var targetIsDestroyed = targetShip == null || targetGo == null;

            Debug.Log($"[E2E Target Practice] Target is destroyed: {targetIsDestroyed}");

            Assert.That(targetIsDestroyed, Is.True,
                "Expected the stationary target ship to be completely destroyed after 10 seconds of targeted gunfire.");
        }
    }
}