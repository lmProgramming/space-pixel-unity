using System;
using Core.Ships;
using Ships.Modules;

namespace Services
{
    public static class SnapshotComponentRegistry
    {
        public static Type ResolveModuleType(ConcreteModuleType moduleType)
        {
            return moduleType switch
            {
                ConcreteModuleType.Command => typeof(Command),
                ConcreteModuleType.Engine => typeof(Engine),
                ConcreteModuleType.Cannon => typeof(Cannon),
                ConcreteModuleType.Basic => typeof(Basic),
                ConcreteModuleType.Laser => typeof(LaserBeam),
                _ => throw new ArgumentOutOfRangeException(nameof(moduleType), moduleType, null)
            };
        }
    }
}