using Core.Ship;
using Ships.Modules;
using UnityEngine;

namespace Ships.Tests.TestHelpers
{
    public sealed class TestPowerModule : Module
    {
        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Resources;
        }

        public override float GetEnergyProduction() => 1000f;
    }
}
