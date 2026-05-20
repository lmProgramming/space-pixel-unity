using Core.Ship;

namespace Core.Services
{
    public interface IShipSnapshotService
    {
        ShipSnapshot CaptureSnapshot(IShip ship);

        void ApplySnapshot(IShip ship, ShipSnapshot snapshot);

        string ToJson(ShipSnapshot snapshot, bool prettyPrint = true);

        ShipSnapshot LoadSnapshotFromFile(string path);
    }
}