using Core.Ship;
using Ships.Modules;

namespace Ships.Tests.TestHelpers.Modules
{
    public sealed class TestPowerModule : Module
    {
        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Resources;
        }

        public override float GetEnergyProduction()
        {
            return 1000f;
        }
    }
}