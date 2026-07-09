using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using UnityEngine;

namespace Ships.Systems.Standalone
{
    public abstract class StandaloneModuleSystem : MonoBehaviour,
        IStandaloneModuleSystem
    {
        public abstract StandaloneModuleSystemData CaptureSnapshot(IGameContentCatalog contentCatalog);

        public abstract void RestoreFromSnapshot(StandaloneModuleSystemData snapshot,
            IGameContentCatalog contentCatalog);
    }
}