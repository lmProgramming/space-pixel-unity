using Ships;
using UnityEngine;

namespace AI.EasyState.States
{
    public class AttackState : BaseState
    {
        private float _attackCooldown;
        private float _attackRange;
        private float _lastAttackTime;
        private Ship _targetEnemy;

        public override string StateName => "Attack";

        public override void Enter(StateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine);

            if (data is AttackStateData attackData)
            {
                _targetEnemy = attackData.TargetEnemy;
                _attackRange = attackData.AttackRange;
                _attackCooldown = attackData.AttackCooldown;
                _lastAttackTime = 0f;

                Debug.Log(
                    $"Attack state entered with target: {_targetEnemy?.name}, range: {_attackRange}, cooldown: {_attackCooldown}");
            }
            else
            {
                Debug.LogWarning("AttackState entered without proper AttackStateData");
            }
        }

        public override void Update(StateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);

            // If no target, transition back to lookout
            if (!_targetEnemy || !_targetEnemy.gameObject.activeInHierarchy)
            {
                stateMachine.TransitionToState("Lookout");
                return;
            }

            var distanceToTarget = Vector2.Distance(stateMachine.transform.position, _targetEnemy.transform.position);

            // If target is out of range, transition to moving state or lookout
            if (distanceToTarget > _attackRange * 1.5f) // Add some buffer
            {
                stateMachine.TransitionToState("Lookout");
                return;
            }

            // Attack if cooldown is ready
            if (Time.time - _lastAttackTime >= _attackCooldown)
            {
                PerformAttack(stateMachine);
                _lastAttackTime = Time.time;
            }
        }

        private void PerformAttack(StateMachine stateMachine)
        {
            Debug.Log($"Attacking {_targetEnemy.name}!");
            stateMachine.ShipController.SetAttackTarget(_targetEnemy.CommandModule.transform.position);
            stateMachine.ShipController.Shoot();
        }

        public override void Exit(StateMachine stateMachine)
        {
            base.Exit(stateMachine);
            _targetEnemy = null;
        }

        public override bool CanTransitionTo(string stateName)
        {
            // Can always transition out of attack state
            return true;
        }
    }
}