using Core.Services;

namespace Core.Snapshot
{
    public interface ISnapshottable<TSnapshot>
    {
        TSnapshot CaptureSnapshot(IGameContentCatalog contentCatalog);
        void RestoreFromSnapshot(TSnapshot snapshot, IGameContentCatalog contentCatalog);
    }
}