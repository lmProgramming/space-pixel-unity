using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using Core.Ships;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Services
{
    public interface IShipService
    {
        IReadOnlyCollection<IShip> GetShips();
        IEnumerable<IShip> GetShipsOfTeam(ITeam team);
        IEnumerable<IShip> GetEnemyShipsOf(ITeam team);
        [CanBeNull] IShip GetClosestEnemyShipOf(ITeam team, Vector2 position);
        IEnumerable<IShip> GetAlliedShipsOf(ITeam team);
        void RegisterShip(IShip ship);
        void UnregisterShip(IShip ship);
    }
}