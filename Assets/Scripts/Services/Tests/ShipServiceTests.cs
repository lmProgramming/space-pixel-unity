using System;
using System.Collections.Generic;
using System.Linq;
using Core.Gameplay.EasyTeam;
using Core.Ship;
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

        [Test]
        public void RegisterShip_AddsShip_ToGetShips()
        {
            var ship = new FakeShip(_teamA, Vector2.zero);

            _shipService.RegisterShip(ship);

            Assert.AreEqual(1, _shipService.GetShips().Count);
            Assert.IsTrue(_shipService.GetShips().Contains(ship));
        }

        [Test]
        public void UnregisterShip_RemovesShip_FromGetShips()
        {
            var ship = new FakeShip(_teamA, Vector2.zero);
            _shipService.RegisterShip(ship);

            _shipService.UnregisterShip(ship);

            Assert.AreEqual(0, _shipService.GetShips().Count);
        }

        [Test]
        public void GetShipsOfTeam_ReturnsOnlyShipsOnThatTeam()
        {
            var shipA = new FakeShip(_teamA, Vector2.zero);
            var shipB = new FakeShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            var teamAShips = _shipService.GetShipsOfTeam(_teamA).ToList();

            Assert.AreEqual(1, teamAShips.Count);
            Assert.Contains(shipA, teamAShips);
        }

        [Test]
        public void GetEnemyShipsOf_ReturnsOnlyEnemyShips()
        {
            var shipA = new FakeShip(_teamA, Vector2.zero);
            var shipB = new FakeShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            var enemies = _shipService.GetEnemyShipsOf(_teamA).ToList();

            Assert.AreEqual(1, enemies.Count);
            Assert.Contains(shipB, enemies);
        }

        [Test]
        public void GetAlliedShipsOf_ReturnsSelf_AndAllies()
        {
            var shipA = new FakeShip(_teamA, Vector2.zero);
            var shipB = new FakeShip(_teamB, Vector2.zero);
            _shipService.RegisterShip(shipA);
            _shipService.RegisterShip(shipB);

            var allies = _shipService.GetAlliedShipsOf(_teamA).ToList();

            Assert.AreEqual(1, allies.Count);
            Assert.Contains(shipA, allies);
        }

        [Test]
        public void GetAlliedShipsOf_ReturnsSelf_AndAlliesTeams()
        {
            var shipA = new FakeShip(_teamA, Vector2.zero);
            var shipB = new FakeShip(_teamB, Vector2.zero);
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
            var nearEnemy = new FakeShip(_teamB, new Vector2(5f, 0f));
            var farEnemy = new FakeShip(_teamB, new Vector2(100f, 0f));
            _shipService.RegisterShip(nearEnemy);
            _shipService.RegisterShip(farEnemy);

            var closest = _shipService.GetClosestEnemyShipOf(_teamA, Vector2.zero);

            Assert.AreEqual(nearEnemy, closest);
        }

        [Test]
        public void GetClosestEnemyShipOf_ReturnsNull_WhenNoEnemies()
        {
            var ally = new FakeShip(_teamA, new Vector2(5f, 0f));
            _shipService.RegisterShip(ally);

            var closest = _shipService.GetClosestEnemyShipOf(_teamA, Vector2.zero);

            Assert.IsNull(closest);
        }

        [Test]
        public void RegisterShip_CalledTwice_DoesNotDuplicate()
        {
            var ship = new FakeShip(_teamA, Vector2.zero);

            _shipService.RegisterShip(ship);
            _shipService.RegisterShip(ship);

            Assert.AreEqual(1, _shipService.GetShips().Count);
        }

        private class FakeShip : IShip
        {
            private readonly Vector2 _position;

            public FakeShip(ITeam team, Vector2 position)
            {
                Team = team;
                _position = position;
            }

            public ITeam Team { get; }
            public IModule CommandModule => null;
            public Collider2D[] OwnColliders => Array.Empty<Collider2D>();

            public Vector2 GetPosition()
            {
                return _position;
            }
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