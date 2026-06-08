using AI.EasyState.States;
using Core.Pixelation;

namespace Ships.StateMachines.AIShip.Data
{
    public class AttackStateData : IStateData
    {
        public readonly float AttackCooldown;
        public readonly float AttackRange;
        public readonly float StopRange;

        public readonly IPixelatedRigidbody TargetEnemy;

        public AttackStateData(IPixelatedRigidbody targetEnemy, float attackRange = 10f, float attackCooldown = 1f,
            float stopRange = 0f)
        {
            TargetEnemy = targetEnemy;
            StopRange = stopRange;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
        }
    }
}