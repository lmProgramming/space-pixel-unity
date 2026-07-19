using Core.Ships;

namespace Core.Services
{
    public interface IActivePlayerShipProvider
    {
        bool HasPlayerShip { get; }

        IShip ActiveShip { get; }

        void SetActiveShip(IShip ship);
    }
}