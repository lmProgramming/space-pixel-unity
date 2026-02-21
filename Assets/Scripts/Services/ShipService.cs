using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Core.Ship;
using UnityEngine;

namespace Services
{
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
            foreach (var ship in _ships)
                if (ship.Team == team)
                    yield return ship;
        }

        public IEnumerable<IShip> GetEnemyShipsOf(ITeam team)
        {
            foreach (var ship in _ships)
                if (team.IsEnemy(ship.Team))
                    yield return ship;
        }

        public IEnumerable<IShip> GetAlliedShipsOf(ITeam team)
        {
            foreach (var ship in _ships)
                if (team.IsAllied(ship.Team))
                    yield return ship;
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