using Core.Ship;

namespace Core.Services
{
    public interface ISkirmishSnapshotCatalog
    {
        ShipSnapshot GetRandomEnemySnapshot();
        ShipSnapshot GetRandomFriendlySnapshot();
        bool HasEnemySnapshots();
        bool HasFriendlySnapshots();
    }
}
