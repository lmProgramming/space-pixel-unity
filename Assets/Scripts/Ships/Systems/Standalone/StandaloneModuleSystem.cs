using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.StandaloneModuleSystemData;
using Ships.Modules;
using UnityEngine;

namespace Ships.Systems.Standalone
{
    public abstract class StandaloneModuleSystem : MonoBehaviour,
        IStandaloneModuleSystem
    {
        protected Module Module;

        protected bool IsDesignMode => Module?.Ship is { IsDesignMode: true };

        private void Awake()
        {
            Module = GetComponent<Module>();
            if (Module == null)
                throw new UnityException($"[{GetType().Name}] Module component is required on the same GameObject.");
        }

        private void FixedUpdate()
        {
            if (IsDesignMode) return;

            TickStandaloneSystem();
        }

        public abstract StandaloneModuleSystemData CaptureSnapshot(IGameContentCatalog contentCatalog);

        public abstract void RestoreFromSnapshot(StandaloneModuleSystemData snapshot,
            IGameContentCatalog contentCatalog);

        protected virtual void TickStandaloneSystem()
        {
        }
    }
}