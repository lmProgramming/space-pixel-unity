using Core.Ship;

namespace Ships.Modules
{
    public class Command : Module
    {
        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Command;
        }
    }
}