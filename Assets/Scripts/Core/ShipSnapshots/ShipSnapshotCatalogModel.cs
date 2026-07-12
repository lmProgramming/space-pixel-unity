using System.Collections.Generic;
using Core.MVCVM;
using Core.Ships;

namespace Core.ShipSnapshots
{
    public class ShipSnapshotCatalogModel : ObservableModel
    {
        private readonly List<SavedShipSnapshotDescriptor> _snapshots = new();

        public IReadOnlyList<SavedShipSnapshotDescriptor> Snapshots => _snapshots;

        public void ReplaceAll(
            IReadOnlyList<SavedShipSnapshotDescriptor> snapshots)
        {
            _snapshots.Clear();

            if (snapshots != null)
                _snapshots.AddRange(snapshots);

            NotifyChanged();
        }
    }
}