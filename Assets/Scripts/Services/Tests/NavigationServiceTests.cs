using System.Collections;
using System.Collections.Generic;
using Core.Constants;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Services.Tests
{
    [TestFixture]
    public class NavigationServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _createdObjects)
                if (obj != null)
                    Object.DestroyImmediate(obj);
            _createdObjects.Clear();
        }

        private const float SectorSize = 10f;
        private readonly List<GameObject> _createdObjects = new();

        [UnityTest]
        public IEnumerator GetSectorResult_ObstacleNearSectorFarCorner_IsBlocked()
        {
            var navigationService = CreateNavigationService();
            CreateObstacle(new Vector2(9f, 9f), new Vector2(1f, 1f));

            yield return null;

            var sectorCenter = new Vector3(5f, 5f, 0f);
            var result = navigationService.GetSectorResult(sectorCenter);

            Assert.IsTrue(result.HasObstacles,
                "Obstacle at (9,9) lies inside sector (0,0) and must block that sector when overlap uses sector center.");
        }

        [UnityTest]
        public IEnumerator GetSectorResult_ObstacleOutsideSectorBounds_IsNotBlocked()
        {
            var navigationService = CreateNavigationService();
            CreateObstacle(new Vector2(-4f, -4f), new Vector2(1f, 1f));

            yield return null;

            var sectorCenter = new Vector3(5f, 5f, 0f);
            var result = navigationService.GetSectorResult(sectorCenter);

            Assert.IsFalse(result.HasObstacles,
                "Obstacle at (-4,-4) lies outside sector (0,0) and must not block that sector.");
        }

        [UnityTest]
        public IEnumerator GetSectorResult_ObstacleInNeighborSector_DoesNotBlockCurrentSector()
        {
            var navigationService = CreateNavigationService();
            CreateObstacle(new Vector2(15f, 5f), new Vector2(1f, 1f));

            yield return null;

            var sectorCenter = new Vector3(5f, 5f, 0f);
            var result = navigationService.GetSectorResult(sectorCenter);

            Assert.IsFalse(result.HasObstacles,
                "Obstacle in sector (10,0) must not mark sector (0,0) as blocked.");
        }

        private NavigationService CreateNavigationService()
        {
            var navigationServiceGo = new GameObject("NavigationService");
            _createdObjects.Add(navigationServiceGo);
            var navigationService = navigationServiceGo.AddComponent<NavigationService>();
            navigationService.InternalSectorSize = SectorSize;
            return navigationService;
        }

        private void CreateObstacle(Vector2 position, Vector2 size)
        {
            var obstacle = new GameObject("Obstacle");
            _createdObjects.Add(obstacle);
            obstacle.transform.position = position;
            obstacle.layer = PhysicsLayers.Obstacles;

            var collider = obstacle.AddComponent<BoxCollider2D>();
            collider.size = size;

            obstacle.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }
    }
}