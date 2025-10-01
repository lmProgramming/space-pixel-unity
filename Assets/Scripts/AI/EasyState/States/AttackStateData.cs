using Ships;

namespace AI.EasyState.States
{
    /// <summary>
    /// Data for attack state transitions
    /// </summary>
    public class AttackStateData : IStateData
    {
        public Ship TargetEnemy { get; set; }
        public float AttackRange { get; set; }
        public float AttackCooldown { get; set; }

        public AttackStateData(Ship targetEnemy, float attackRange = 10f, float attackCooldown = 1f)
        {
            TargetEnemy = targetEnemy;
            AttackRange = attackRange;
            AttackCooldown = attackCooldown;
        }
    }
}