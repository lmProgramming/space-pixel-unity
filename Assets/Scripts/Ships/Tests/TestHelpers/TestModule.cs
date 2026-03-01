using Core.Ship;
using Ships.Modules;

namespace Ships.Tests.TestHelpers
{
    public class TestModule : Module
    {
        public void SetModuleType(ModuleType type)
        {
            Type = type;
        }

        public void SetMainSkillType(CrewSkillType skillType)
        {
            MainSkillTypeForTesting = skillType;
        }

        public void SetShip(IShip ship)
        {
            Ship = ship;
        }
    }
}