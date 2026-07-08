using Core.Ships.Snapshots.Module;
using UnityEngine;

namespace Core.Services
{
    public interface IModuleRestoreFactory
    {
        GameObject CreateModuleObject(ModuleSnapshot snapshot, Transform parent);
    }
}