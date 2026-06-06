using System.Collections.Generic;
using Core.Services;
using Core.Ship;
using Gameplay.Navigation;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Gameplay.Tests.Navigation
{
    [TestFixture]
    public class NavigationCalculatorTests
    {
        private const float SectorSize = 10f;

        private static NavigationCalculator BuildCalculator(HashSet<Vector2> blockedSectors = null)
        {
            blockedSectors ??= new HashSet<Vector2>();
            return new NavigationCalculator(SectorSize, Query, (sector, _) => Query(sector));

            SectorResult Query(Vector2 sector)
            {
                return new SectorResult(blockedSectors.Contains(sector), false, 0f);
            }
        }

        private static NavigationCalculator BuildCalculatorWithShipAwareness(
            Dictionary<Vector2, IReadOnlyCollection<IShip>> shipsPerSector)
        {
            return new NavigationCalculator(SectorSize, SimpleQuery, ShipAwareQuery);

            SectorResult SimpleQuery(Vector2 sector)
            {
                return SectorResult.Empty;
            }

            SectorResult ShipAwareQuery(Vector2 sector, IShip _)
            {
                var ships = shipsPerSector.GetValueOrDefault(sector);
                return new SectorResult(false, false, 0f, ships);
            }
        }

        [Test]
        public void NormalizePositionToSector_SnapsToSectorOrigin()
        {
            var calculator = BuildCalculator();

            var result = calculator.NormalizePositionToSector(new Vector3(13.7f, 28.2f));

            Assert.AreEqual(new Vector2(10f, 20f), result);
        }

        [Test]
        public void NormalizePositionToSector_NegativeCoordinates_SnapsCorrectly()
        {
            var calculator = BuildCalculator();

            var result = calculator.NormalizePositionToSector(new Vector3(-3f, -15f));

            Assert.AreEqual(new Vector2(-10f, -20f), result);
        }

        [Test]
        public void GetSectorCenter_ReturnsMidpointOfSector()
        {
            var calculator = BuildCalculator();
            var sectorOrigin = new Vector2(10f, 20f);

            var center = calculator.GetSectorCenter(sectorOrigin);

            Assert.AreEqual(new Vector2(15f, 25f), center);
        }

        [Test]
        public void CalculatePath_StartAndEndInSameSector_ReturnsSingleWaypoint()
        {
            var calculator = BuildCalculator();
            var start = new Vector3(12f, 14f);
            var end = new Vector3(17f, 19f);

            var path = calculator.CalculatePath(start, end, 5, int.MaxValue);

            Assert.IsNotNull(path);
            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(calculator.GetSectorCenter(calculator.NormalizePositionToSector(end)), (Vector2)path[0]);
        }

        [Test]
        public void CalculatePath_ClearHorizontalCorridor_ReturnsNonNullPath()
        {
            var calculator = BuildCalculator();
            var start = new Vector3(5f, 5f);
            var end = new Vector3(35f, 5f);

            var path = calculator.CalculatePath(start, end, 5, int.MaxValue);

            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0);
        }

        [Test]
        public void CalculatePath_AllNeighborsBlocked_ReturnsNull()
        {
            var blocked = new HashSet<Vector2>
            {
                new(SectorSize, 0f),
                new(-SectorSize, 0f),
                new(0f, SectorSize),
                new(0f, -SectorSize)
            };
            var calculator = BuildCalculator(blocked);

            var path = calculator.CalculatePath(new Vector3(5f, 5f), new Vector3(50f, 50f), 5, int.MaxValue);

            Assert.IsNull(path);
        }

        [Test]
        public void CalculatePath_AllNeighborsBlockedIfShipBig_ReturnsNull()
        {
            const float twoSectorsAway = SectorSize * 2;
            var blocked = new HashSet<Vector2>
            {
                new(twoSectorsAway, 0f),
                new(-twoSectorsAway, 0f),
                new(0f, twoSectorsAway),
                new(0f, -twoSectorsAway),
                new(twoSectorsAway, twoSectorsAway),
                new(-twoSectorsAway, twoSectorsAway),
                new(twoSectorsAway, -twoSectorsAway),
                new(-twoSectorsAway, -twoSectorsAway)
            };
            var calculator = BuildCalculator(blocked);

            var pathForSmallShip =
                calculator.CalculatePath(new Vector3(5f, 5f), new Vector3(1000f, 1000f), (int)SectorSize, int.MaxValue);
            Assert.IsNotNull(pathForSmallShip, "Expected a path for small ship since direct neighbors are clear");

            var pathForBigShip =
                calculator.CalculatePath(new Vector3(5f, 5f), new Vector3(1000f, 1000f), (int)SectorSize * 2,
                    int.MaxValue);

            Assert.IsNull(pathForBigShip,
                "Expected no path for big ship since all neighbors are blocked for its footprint");
        }

        [Test]
        public void CalculatePath_PathGoesAroundBlockedSector()
        {
            // Block the direct route (sector above start) so path must go around
            var directRoute = new Vector2(0f, SectorSize);
            var blocked = new HashSet<Vector2> { directRoute };
            var calculator = BuildCalculator(blocked);

            var start = new Vector3(5f, 5f);
            var end = new Vector3(5f, 35f);

            var path = calculator.CalculatePath(start, end, 5, int.MaxValue);

            Assert.IsNotNull(path, "Expected a path around the obstacle");
            foreach (var waypoint in path)
                Assert.AreNotEqual(directRoute, calculator.NormalizePositionToSector(waypoint),
                    "Path should not pass through the blocked sector");
        }

        [Test]
        public void CalculatePath_DoesNotIncludeStartSector()
        {
            var calculator = BuildCalculator();
            var start = new Vector3(5f, 5f);
            var end = new Vector3(35f, 5f);

            var path = calculator.CalculatePath(start, end, 5, int.MaxValue);

            Assert.IsNotNull(path);
            var startSectorCenter = calculator.GetSectorCenter(calculator.NormalizePositionToSector(start));
            foreach (var waypoint in path)
                Assert.AreNotEqual((Vector2)waypoint, startSectorCenter,
                    "Path should not contain the starting sector's waypoint");
        }

        [Test]
        public void CalculatePath_LargeShip_OnlyOneCorridor_StillFindsPath()
        {
            // Ship size = 2 * SectorSize → footprintRadius = 2, so navigability checks a 3×3 block of sectors.
            // Block three of the four cardinal neighbors by placing an obstacle inside each of their 3×3 footprints
            // without touching the right-hand corridor (neighbor at (+SectorSize, 0)).
            //   Left neighbor footprint contains (-2S, 0) → block it
            //   Up neighbor footprint contains (0, +2S)  → block it
            //   Down neighbor footprint contains (0, -2S)  → block it
            var blocked = new HashSet<Vector2>
            {
                new(-2 * SectorSize, 0f),
                new(0f, 2 * SectorSize),
                new(0f, -2 * SectorSize)
            };
            var calculator = BuildCalculator(blocked);
            var shipSize = (int)(SectorSize * 2);

            var path = calculator.CalculatePath(
                new Vector3(5f, 5f), new Vector3(100f, 5f), shipSize, int.MaxValue);

            Assert.IsNotNull(path, "Large ship should find the one open corridor to the right");
        }

        [Test]
        public void CalculatePath_LargeShip_AllCorridorsBlocked_ReturnsNull()
        {
            // Block all four corridors for a large ship (footprintRadius 2) by placing one obstacle
            // inside each of the four 3×3 footprint regions.
            var blocked = new HashSet<Vector2>
            {
                new(-2 * SectorSize, 0f),
                new(2 * SectorSize, 0f),
                new(0f, 2 * SectorSize),
                new(0f, -2 * SectorSize)
            };
            var calculator = BuildCalculator(blocked);
            var shipSize = (int)(SectorSize * 2);

            var path = calculator.CalculatePath(
                new Vector3(5f, 5f), new Vector3(100f, 100f), shipSize, int.MaxValue);

            Assert.IsNull(path, "Large ship should find no path when all corridors are blocked");
        }

        [Test]
        public void CalculatePath_ShipAware_ThirdShipWithDebrisBlocksPath()
        {
            var callerShip = Substitute.For<IShip>();
            var targetShip = Substitute.For<IShip>();
            var thirdShip = Substitute.For<IShip>();

            // Sectors contain a third ship AND debris — both conditions independently block, together they definitely block
            var allNeighbors = new Dictionary<Vector2, SectorResult>
            {
                { new Vector2(SectorSize, 0f), new SectorResult(false, true, 0f, new[] { thirdShip }) },
                { new Vector2(-SectorSize, 0f), new SectorResult(false, true, 0f, new[] { thirdShip }) },
                { new Vector2(0f, SectorSize), new SectorResult(false, true, 0f, new[] { thirdShip }) },
                { new Vector2(0f, -SectorSize), new SectorResult(false, true, 0f, new[] { thirdShip }) }
            };

            var calculator = new NavigationCalculator(SectorSize,
                _ => SectorResult.Empty,
                (sector, _) => allNeighbors.TryGetValue(sector, out var r) ? r : SectorResult.Empty);

            var path = calculator.CalculatePath(
                new Vector3(5f, 5f), new Vector3(50f, 50f), 5, callerShip, targetShip.CommandModule.PixelatedRigidbody,
                int.MaxValue);

            Assert.IsNull(path, "Expected no path when all neighboring sectors have both a third ship and debris");
        }

        [Test]
        public void CalculatePath_ShipAware_ThirdShipInSectorBlocksPath()
        {
            var callerShip = Substitute.For<IShip>();
            var targetShip = Substitute.For<IShip>();
            var thirdShip = Substitute.For<IShip>();

            // Place the third ship in the only direct route sector (0, SectorSize)
            // With only one neighbor available and it is blocked by a third ship,
            // surround start sector so every escape route contains the third ship
            var allNeighbors = new Dictionary<Vector2, IReadOnlyCollection<IShip>>
            {
                { new Vector2(SectorSize, 0f), new[] { thirdShip } },
                { new Vector2(-SectorSize, 0f), new[] { thirdShip } },
                { new Vector2(0f, SectorSize), new[] { thirdShip } },
                { new Vector2(0f, -SectorSize), new[] { thirdShip } }
            };
            var blockedCalculator = BuildCalculatorWithShipAwareness(allNeighbors);

            var path = blockedCalculator.CalculatePath(
                new Vector3(5f, 5f), new Vector3(50f, 50f), 5, callerShip, targetShip.CommandModule.PixelatedRigidbody,
                int.MaxValue);

            Assert.IsNull(path, "Expected no path when all neighboring sectors contain a third ship");
        }

        [Test]
        public void CalculatePath_ShipAware_CallerAndTargetShipsDoNotBlockPath()
        {
            var callerShip = Substitute.For<IShip>();
            var targetShip = Substitute.For<IShip>();

            // The only route forward (0, SectorSize) contains just the caller and target ships — must be passable
            var directRouteSector = new Vector2(0f, SectorSize);
            var shipsPerSector = new Dictionary<Vector2, IReadOnlyCollection<IShip>>
            {
                { directRouteSector, new[] { callerShip, targetShip } }
            };
            var calculator = BuildCalculatorWithShipAwareness(shipsPerSector);

            var path = calculator.CalculatePath(
                new Vector3(5f, 5f), new Vector3(5f, 35f), 5, callerShip, targetShip.CommandModule.PixelatedRigidbody,
                int.MaxValue);

            Assert.IsNotNull(path, "Expected a path when the sector contains only caller and target ships");
        }
    }
}