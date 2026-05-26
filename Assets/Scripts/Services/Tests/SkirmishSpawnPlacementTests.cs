using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Services.Tests
{
    [TestFixture]
    public class SkirmishSpawnPlacementTests
    {
        [Test]
        public void TryFindPosition_ReturnsFalse_WhenSpawnAreaTooSmallForRadius()
        {
            var spawnRect = Rect.MinMaxRect(0f, 0f, 10f, 10f);

            var result = SkirmishSpawnPlacement.TryFindPosition(
                spawnRect,
                6f,
                new List<SkirmishSpawnPlacement.SpawnReservation>(),
                3,
                0,
                out _,
                (_, _) => new Vector2(5f, 5f),
                (_, _) => false);

            Assert.IsFalse(result);
        }

        [Test]
        public void TryFindPosition_SkipsOverlappingReservations_AndUsesLaterCandidate()
        {
            var spawnRect = Rect.MinMaxRect(0f, 0f, 100f, 100f);
            var reservations = new List<SkirmishSpawnPlacement.SpawnReservation>
            {
                new(new Vector2(10f, 10f), 8f)
            };
            var candidates = new Queue<Vector2>(new[]
            {
                new Vector2(10f, 10f),
                new Vector2(80f, 80f)
            });

            var result = SkirmishSpawnPlacement.TryFindPosition(
                spawnRect,
                8f,
                reservations,
                2,
                0,
                out var chosenPosition,
                (_, _) => candidates.Dequeue(),
                (_, _) => false);

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2(80f, 80f), chosenPosition);
        }

        [Test]
        public void TryFindPosition_RespectsBlockingCheck()
        {
            var spawnRect = Rect.MinMaxRect(0f, 0f, 100f, 100f);
            var candidates = new Queue<Vector2>(new[]
            {
                new Vector2(20f, 20f),
                new Vector2(70f, 70f)
            });

            var result = SkirmishSpawnPlacement.TryFindPosition(
                spawnRect,
                4f,
                new List<SkirmishSpawnPlacement.SpawnReservation>(),
                2,
                0,
                out var chosenPosition,
                (_, _) => candidates.Dequeue(),
                (point, _) => point == new Vector2(20f, 20f));

            Assert.IsTrue(result);
            Assert.AreEqual(new Vector2(70f, 70f), chosenPosition);
        }
    }
}
