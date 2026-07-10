using Core.Services;

namespace Core.Snapshot
{
    public interface ISnapshottable<TSnapshot>
    {
        public TSnapshot CaptureSnapshot(IGameContentCatalog contentCatalog);
        public void RestoreFromSnapshot(TSnapshot snapshot, IGameContentCatalog contentCatalog);
    }
}