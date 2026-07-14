using System;
using Core.Ships;

namespace Core.Gameplay.Progression
{
    [Serializable]
    public class ProgressionSave
    {
        public string campaignName;
        public ShipSnapshot[] allies;
        public int enemiesKilled;
        public string credits;
    }
}