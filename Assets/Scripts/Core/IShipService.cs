using System.Collections.Generic;

namespace Core
{
    public interface IShipService
    {
        IEnumerable<IShip> GetShips();
        IEnumerable<IShip> GetShipsOfTeam(Team team);
        IEnumerable<IShip> GetEnemyShipsOf(Team team);
        IEnumerable<IShip> GetAlliedShipsOf(Team team);
        void RegisterShip(IShip ship);
        void UnregisterShip(IShip ship);
    }
}