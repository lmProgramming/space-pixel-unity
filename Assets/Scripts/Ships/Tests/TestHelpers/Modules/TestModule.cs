using Core.Ship;
using Ships.Modules;

namespace Ships.Tests.TestHelpers.Modules
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
    }
}