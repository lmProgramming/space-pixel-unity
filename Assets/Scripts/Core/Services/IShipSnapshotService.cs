using Core.Ships;

namespace Core.Services
{
    public interface IShipSnapshotService
    {
        ShipSnapshot CaptureSnapshot(IShip ship);

        void ApplySnapshot(IShip ship, ShipSnapshot snapshot);

        ShipSnapshot LoadSnapshotFromFile(string path);
    }
}