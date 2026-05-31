using AI.EasyState.States;
using Core.Pixelation;

namespace Ships.StateMachines.AIShip.Data
{
    public class AttackStateData : IStateData
    {
        public readonly float AttackCooldown;
        public readonly float AttackRange;

        public readonly IPixelatedRigidbody TargetEnemy;

        public AttackStateData(IPixelatedRigidbody targetEnemy, float attackRange = 10f, float attackCooldown = 1f)
        {
            TargetEnemy = targetEnemy;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
        }
    }
}