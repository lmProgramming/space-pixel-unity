using Core.Ships;
using ZLinq;

namespace Ships.Modules
{
    public class Command : Module
    {
        public override ModuleType Type => ModuleType.Command;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Command;
        }

        public override float GetCrewEfficiency()
        {
            var captainTotal = AliveCrew.AsValueEnumerable()
                .Sum(crew => crew.GetSkillLevel(CrewSkillType.Captain));

            return 1f + captainTotal * CaptainBonusPerLevel;
        }
    }
}