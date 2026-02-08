using AI.EasyState.States;
using Core.Ship;

namespace Ships.AIStates
{
    public class AttackStateData : IStateData
    {
        public AttackStateData(IShip targetEnemy, float attackRange = 10f, float attackCooldown = 1f)
        {
            TargetEnemy = targetEnemy;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
        }

        public IShip TargetEnemy { get; set; }
        public float AttackRange { get; set; }
        public float AttackCooldown { get; set; }
    }
}