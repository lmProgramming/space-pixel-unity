using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.Systems;
using UnityEngine;

namespace Ships.Systems.Standalone
{
    public abstract class StandaloneModuleSystem : MonoBehaviour,
        IStandaloneModuleSystem
    {
        public abstract SystemData CaptureSnapshot(IGameContentCatalog contentCatalog);

        public abstract void RestoreFromSnapshot(SystemData snapshot, IGameContentCatalog contentCatalog);
    }
}