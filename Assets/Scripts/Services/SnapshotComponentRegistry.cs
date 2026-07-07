using System;
using Core.Ships;
using Ships.Modules;

namespace Services
{
    public static class SnapshotComponentRegistry
    {
        public static Type ResolveModuleType(string moduleTypeName, ModuleType moduleType)
        {
            if (!string.IsNullOrEmpty(moduleTypeName))
                return moduleTypeName switch
                {
                    nameof(Command) => typeof(Command),
                    nameof(Engine) => typeof(Engine),
                    nameof(Cannon) => typeof(Cannon),
                    nameof(LaserBeam) => typeof(LaserBeam),
                    nameof(Basic) => typeof(Basic),
                    _ => ResolveModuleTypeFromEnum(moduleType)
                };

            return ResolveModuleTypeFromEnum(moduleType);
        }

        private static Type ResolveModuleTypeFromEnum(ModuleType moduleType)
        {
            return moduleType switch
            {
                ModuleType.Command => typeof(Command),
                ModuleType.Engine => typeof(Engine),
                ModuleType.Weapon => typeof(Cannon),
                _ => typeof(Basic)
            };
        }
    }
}