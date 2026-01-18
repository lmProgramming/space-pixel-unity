using System.Collections.Generic;
using Core.Gameplay.EasyTeam;
using Core.Ship;

namespace Core.Services
{
    public interface IShipService
    {
        IEnumerable<IShip> GetShips();
        IEnumerable<IShip> GetShipsOfTeam(ITeam team);
        IEnumerable<IShip> GetEnemyShipsOf(ITeam team);
        IEnumerable<IShip> GetAlliedShipsOf(ITeam team);
        void RegisterShip(IShip ship);
        void UnregisterShip(IShip ship);
    }
}