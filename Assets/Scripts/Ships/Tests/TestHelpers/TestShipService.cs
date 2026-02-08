using System.Collections.Generic;
using System.Linq;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Core.Ship;

namespace Ships.Tests.TestHelpers
{
    public class TestShipService : IShipService
    {
        private readonly List<IShip> _ships = new();

        public IEnumerable<IShip> GetShips()
        {
            return _ships;
        }

        public IEnumerable<IShip> GetShipsOfTeam(ITeam team)
        {
            return _ships.Where(s => s.Team == team);
        }

        public IEnumerable<IShip> GetEnemyShipsOf(ITeam team)
        {
            return _ships.Where(s => s.Team != team);
        }

        public IEnumerable<IShip> GetAlliedShipsOf(ITeam team)
        {
            return _ships.Where(s => s.Team == team);
        }

        public void RegisterShip(IShip ship)
        {
            if (!_ships.Contains(ship))
                _ships.Add(ship);
        }

        public void UnregisterShip(IShip ship)
        {
            _ships.Remove(ship);
        }
    }
}