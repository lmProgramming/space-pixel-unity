using System;

namespace Core.Ships.Snapshots.Module.StandaloneModuleSystemData
{
    [Serializable]
    public class ReactionWheelData : StandaloneModuleSystemData
    {
        public ReactionWheelSettings data;

        public ReactionWheelData()
        {
            type = StandaloneModuleSystemType.ReactionWheel;
        }
    }
}