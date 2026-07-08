using System;
using Core.Ships;
using Ships.Modules;

namespace Services
{
    public static class SnapshotComponentRegistry
    {
        public static Type ResolveModuleType(ModuleType moduleType)
        {
            return moduleType switch
            {
                ModuleType.Command => typeof(Command),
                ModuleType.Engine => typeof(Engine),
                ModuleType.Weapon => typeof(Cannon),
                ModuleType.Resources => typeof(Basic),
                ModuleType.Structural => typeof(Basic),
                _ => throw new ArgumentOutOfRangeException(nameof(moduleType), moduleType, null)
            };
        }
    }
}