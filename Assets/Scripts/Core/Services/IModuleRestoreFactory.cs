using Core.Ships.Snapshots.Module;
using UnityEngine;

namespace Core.Services
{
    public interface IModuleRestoreFactory
    {
        GameObject CreateModuleShell(ModuleSnapshot snapshot, Transform parent);
    }
}