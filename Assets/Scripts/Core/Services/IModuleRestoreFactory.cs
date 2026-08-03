using Core.Ships.Blueprints;
using Core.Ships.Snapshots.Module;
using UnityEngine;

namespace Core.Services
{
    public interface IModuleRestoreFactory
    {
        GameObject CreateModuleShell(ModuleSnapshot snapshot, Transform parent);
        GameObject CreateModuleShellFromBlueprint(ModuleBlueprint blueprint, Transform parent);
    }
}