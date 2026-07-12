using Core.Ships;
using Core.ShipSnapshots;

namespace Core.Services
{
    public interface IShipSnapshotRepository
    {
        ShipSnapshotCatalogModel Model { get; }

        void SaveSnapshot(ShipSnapshot snapshot);

        bool SnapshotExists(string shipName);

        void DeleteSnapshot(string filePath);
    }
}