using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace Services
{
    public class ShipService : MonoBehaviour, IShipService
    {
        private readonly HashSet<IShip> _ships = new();

        public IEnumerable<IShip> GetShips()
        {
            return _ships;
        }

        public IEnumerable<IShip> GetShipsOfTeam(Team team)
        {
            return _ships.Where(ship => ship.Team == team);
        }

        public IEnumerable<IShip> GetEnemyShipsOf(Team team)
        {
            return _ships.Where(ship => team.IsEnemy(ship.Team));
        }

        public IEnumerable<IShip> GetAlliedShipsOf(Team team)
        {
            return _ships.Where(ship => team.IsAllied(ship.Team));
        }

        public void RegisterShip(IShip ship)
        {
            _ships.Add(ship);
        }

        public void UnregisterShip(IShip ship)
        {
            _ships.Remove(ship);
        }
    }
}