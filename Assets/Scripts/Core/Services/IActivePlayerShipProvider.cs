using Core.Ships;

namespace Core.Services
{
    public interface IActivePlayerShipProvider
    {
        IShip ActiveShip { get; }

        void SetActiveShip(IShip ship);
    }
}