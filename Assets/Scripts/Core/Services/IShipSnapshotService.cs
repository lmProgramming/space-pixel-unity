using System.Collections.Generic;
using Core.Ships;

namespace Core.Services
{
    public interface IShipSnapshotService
    {
        ShipSnapshot CaptureSnapshot(IShip ship);

        void ApplySnapshot(IShip ship, ShipSnapshot snapshot);

        ShipSnapshot LoadSnapshotFromFile(string path);

        IReadOnlyList<SavedShipSnapshotDescriptor> GetSavedSnapshots(
            string folderPath = null);

        void DeleteSnapshotFile(string snapshotPath);
    }
}