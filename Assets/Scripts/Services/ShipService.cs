using System.Collections.Generic;
using System.Linq;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Core.Ship;
using UnityEngine;

namespace Services
{
    // uses Linq on purpose as ZLinq here doesn't make sense for method return types
    public class ShipService : MonoBehaviour, IShipService
    {
        private readonly HashSet<IShip> _ships = new();

        public IReadOnlyCollection<IShip> GetShips()
        {
            return _ships;
        }

        public void RegisterShip(IShip ship)
        {
            _ships.Add(ship);
        }

        public void UnregisterShip(IShip ship)
        {
            _ships.Remove(ship);
        }

        public IEnumerable<IShip> GetShipsOfTeam(ITeam team)
        {
            return _ships.Where(ship => ship.Team == team);
        }

        public IEnumerable<IShip> GetEnemyShipsOf(ITeam team)
        {
            return _ships.Where(ship => team.IsEnemy(ship.Team));
        }

        public IEnumerable<IShip> GetAlliedShipsOf(ITeam team)
        {
            return _ships.Where(ship => team.IsAllied(ship.Team));
        }

        public IShip GetClosestEnemyShipOf(ITeam team, Vector2 position)
        {
            IShip closest = null;
            var bestDist = float.MaxValue;

            foreach (var ship in _ships)
            {
                if (!team.IsEnemy(ship.Team))
                    continue;

                var d = (ship.GetPosition() - position).sqrMagnitude;
                if (!(d < bestDist)) continue;
                bestDist = d;
                closest = ship;
            }

            return closest;
        }
    }
}