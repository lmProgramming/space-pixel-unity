using Core.Ships;

namespace Core.Services
{
    public interface ISkirmishSnapshotCatalog
    {
        ShipSnapshot GetRandomEnemySnapshot();
        ShipSnapshot GetRandomFriendlySnapshot();
        ShipSnapshot[] GetRandomEnemySnapshots(int count);
        ShipSnapshot[] GetRandomFriendlySnapshots(int count);
        bool HasEnemySnapshots();
        bool HasFriendlySnapshots();
    }
}