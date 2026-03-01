using System.Collections.Generic;
using System.Linq;
using Core.Gameplay.EasyTeam;
using Core.Ship;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Services.Tests
{
    [TestFixture]
    public class ShipServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            _serviceGo = new GameObject("ShipService");
            _shipService = _serviceGo.AddComponent<ShipService>();

            _teamA = new FakeTeam("A");
            _teamB = new FakeTeam("B");
            _teamA.AddEnemy(_teamB);
            _teamB.AddEnemy(_teamA);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_serviceGo);
        }

        private GameObject _serviceGo;
        private ShipService _shipService;
        private FakeTeam _teamA;
        private FakeTeam _teamB;

        [Test]
        public void GetShips_ReturnsEmpty_WhenNoShipsRegistered()
        {
            Assert.AreEqual(0, _shipService.GetShips().Count);
        }

        private static IShip CreateMockShip(ITeam team, Vector2 position)
        {
            var ship = Substitute.For<IShip>();
            ship.Team.Returns(team);
            ship.GetPosition().Returns(position);
            return ship;
        }

        [Test]
        public void RegisterShip_AddsShip_ToGetShips()
        {
            var ship = CreateMockShip(_teamA, Vector2.zero);

            _shipService.RegisterShip(ship);

            Assert.AreEqual(1, _shipService.GetShips().Count);
            Assert.IsTrue(_shipService.GetShips().Contains(ship));
        }

        [Test]
        public void UnregisterShip_RemovesShip_FromGetShips()
        {
            var ship = CreateMockShip(_teamA, Vector2.zero);
            _shipService.RegisterShip(ship);

            _shipService.UnregisterShip(ship);

            Assert.AreEqual(0, _shipService.GetShips().Count);
        }

        [Test]
        public void GetShipsOfTeam_ReturnsOnlyShipsOnThatTeam()
        {
            var shipA = CreateMockShip(_teamA, Vector2.zero);
            var shipB = CreateMockShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            var teamAShips = _shipService.GetShipsOfTeam(_teamA).ToList();

            Assert.AreEqual(1, teamAShips.Count);
            Assert.Contains(shipA, teamAShips);
        }

        [Test]
        public void GetEnemyShipsOf_ReturnsOnlyEnemyShips()
        {
            var shipA = CreateMockShip(_teamA, Vector2.zero);
            var shipB = CreateMockShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            var enemies = _shipService.GetEnemyShipsOf(_teamA).ToList();

            Assert.AreEqual(1, enemies.Count);
            Assert.Contains(shipB, enemies);
        }

        [Test]
        public void GetAlliedShipsOf_ReturnsSelf_AndAllies()
        {
            var shipA = CreateMockShip(_teamA, Vector2.zero);
            var shipB = CreateMockShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            var allies = _shipService.GetAlliedShipsOf(_teamA).ToList();

            Assert.AreEqual(1, allies.Count);
            Assert.Contains(shipA, allies);
        }

        [Test]
        public void GetAlliedShipsOf_ReturnsSelf_AndAlliesTeams()
        {
            var shipA = CreateMockShip(_teamA, Vector2.zero);
            var shipB = CreateMockShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            _teamA.AddAlly(_teamB);

            var allies = _shipService.GetAlliedShipsOf(_teamA).ToList();

            Assert.AreEqual(2, allies.Count);
            Assert.Contains(shipA, allies);
        }

        [Test]
        public void GetClosestEnemyShipOf_ReturnsClosestEnemy()
        {
            var nearEnemy = CreateMockShip(_teamB, new Vector2(5f, 0f));
            var farEnemy = CreateMockShip(_teamB, new Vector2(100f, 0f));
            _shipService.RegisterShip(nearEnemy);
            _shipService.RegisterShip(farEnemy);

            var closest = _shipService.GetClosestEnemyShipOf(_teamA, Vector2.zero);

            Assert.AreEqual(nearEnemy, closest);
        }

        [Test]
        public void GetClosestEnemyShipOf_ReturnsNull_WhenNoEnemies()
        {
            var ally = CreateMockShip(_teamA, new Vector2(5f, 0f));
            _shipService.RegisterShip(ally);

            var closest = _shipService.GetClosestEnemyShipOf(_teamA, Vector2.zero);

            Assert.IsNull(closest);
        }

        [Test]
        public void RegisterShip_CalledTwice_DoesNotDuplicate()
        {
            var ship = CreateMockShip(_teamA, Vector2.zero);

            _shipService.RegisterShip(ship);
            _shipService.RegisterShip(ship);

            Assert.AreEqual(1, _shipService.GetShips().Count);
        }

        private class FakeTeam : ITeam
        {
            private readonly List<FakeTeam> _allies = new();
            private readonly List<FakeTeam> _enemies = new();
            private readonly string _name;

            public FakeTeam(string name)
            {
                _name = name;
            }

            public bool IsAllied(ITeam other)
            {
                return other == this || _allies.Contains(other as FakeTeam);
            }

            public bool IsEnemy(ITeam other)
            {
                return _enemies.Contains(other as FakeTeam);
            }

            public void AddEnemy(FakeTeam team)
            {
                _enemies.Add(team);
            }

            public void AddAlly(FakeTeam team)
            {
                _allies.Add(team);
            }

            public override string ToString()
            {
                return _name;
            }
        }
    }
}