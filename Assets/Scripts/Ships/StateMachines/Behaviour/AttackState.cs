using System;
using AI.EasyState.States;
using Core.Ship;
using UnityEngine;

namespace Ships.StateMachines.Behaviour
{
    public class AttackState : BehaviourState
    {
        private float _attackCooldown;
        private float _attackRange;
        private float _lastAttackTime;
        private IShip _targetEnemy;

        public override string StateName => "Attack";

        public override void Enter(BehaviourStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);

            if (data is AttackStateData attackData)
            {
                _targetEnemy = attackData.TargetEnemy;
                _attackRange = attackData.AttackRange;
                _attackCooldown = attackData.AttackCooldown;
                _lastAttackTime = 0f;
            }
            else
            {
                throw new ArgumentException("AttackState requires AttackStateData");
            }
        }

        public override void Update(BehaviourStateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);

            // If no target, transition back to lookout
            if (_targetEnemy == null)
            {
                stateMachine.TransitionToState("Lookout");
                return;
            }

            var distanceToTarget = Vector2.Distance(stateMachine.transform.position, _targetEnemy.GetPosition());

            // If target is out of range, transition to moving state or lookout
            if (distanceToTarget > _attackRange * 1.5f) // Add some buffer
            {
                stateMachine.TransitionToState("Lookout");
                return;
            }

            // Attack if cooldown is ready
            if (Time.time - _lastAttackTime >= _attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
        }

        private void PerformAttack()
        {
            Ship.SetAttackTarget(_targetEnemy.CommandModule.Transform.position);
            Ship.Shoot();
        }

        public override void Exit(BehaviourStateMachine stateMachine)
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