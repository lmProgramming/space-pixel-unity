using Core.Ships;
using Core.ShipSnapshots;

namespace Core.Services
{
    public interface IShipSnapshotRepository
    {
        ShipSnapshotCatalogModel Model { get; }

        void SaveSnapshot(ShipSnapshot snapshot);

        void DeleteSnapshot(string filePath);
    }
}