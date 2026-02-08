using AI.EasyState.States;
using Core.Ship;

namespace Ships.StateMachines.Navigation
{
    public record EnemyTargetStateData : IStateData
    {
        public readonly float DistanceThreshold;

        public readonly IShip TargetEnemy;

        public EnemyTargetStateData(IShip targetEnemy, float distanceThreshold = 50f)
        {
            TargetEnemy = targetEnemy;
            DistanceThreshold = distanceThreshold;
        }
    }
}