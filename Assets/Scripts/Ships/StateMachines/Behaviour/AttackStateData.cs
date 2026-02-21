using AI.EasyState.States;
using Core.Ship;

namespace Ships.StateMachines.Behaviour
{
    public class AttackStateData : IStateData
    {
        public readonly float AttackCooldown;
        public readonly float AttackRange;

        public readonly IShip TargetEnemy;

        public AttackStateData(IShip targetEnemy, float attackRange = 10f, float attackCooldown = 1f)
        {
            TargetEnemy = targetEnemy;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
        }
    }
}