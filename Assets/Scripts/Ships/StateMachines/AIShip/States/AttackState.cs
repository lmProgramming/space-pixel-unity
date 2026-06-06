using System;
using AI.EasyState.States;
using Core.Pixelation;
using LMPro.External.IsAlive;
using Ships.StateMachines.AIShip.Data;
using UnityEngine;

namespace Ships.StateMachines.AIShip.States
{
    public class AttackState : AIShipState
    {
        private const float AttackStopBufferRangeMultiplier = 1.5f;

        private float _attackCooldown;
        private float _attackRange;
        private float _lastAttackTime;

        private float _navigationTargetDistanceThreshold;

        private IPixelatedRigidbody _targetEnemy;

        public override string StateName => "Attack";

        public override void Enter(AIShipStateMachine stateMachine, IStateData data)
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

            NavigationFollower = new NavigationFollower(Ship, stateMachine.NavigationService.SectorSize)
                .WithTarget(_targetEnemy);
        }

        public override void Update(AIShipStateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);
            UpdateAttack(stateMachine);
            UpdateNavigation(stateMachine);
        }

        private void UpdateAttack(AIShipStateMachine stateMachine)
        {
            // If no target, transition back to lookout
            if (!_targetEnemy.IsAlive())
            {
                stateMachine.TransitionToState("Lookout");
                return;
            }

            var distanceToTarget = Vector2.Distance(Ship.GetPosition(), _targetEnemy.WorldWeightedCenter);

            if (distanceToTarget > _attackRange * AttackStopBufferRangeMultiplier)
            {
                stateMachine.TransitionToState("Lookout");
                return;
            }

            if (Time.time - _lastAttackTime < _attackCooldown) return;

            PerformAttack();
            _lastAttackTime = Time.time;
        }

        private void UpdateNavigation(AIShipStateMachine stateMachine)
        {
            if (_targetEnemy == null)
            {
                stateMachine.ClearMovementTarget();
                return;
            }

            var targetPosition = _targetEnemy.WorldWeightedCenter;
            var distanceToTarget = Vector2.Distance(Ship.GetPosition(), targetPosition);

            if (distanceToTarget <= _navigationTargetDistanceThreshold)
            {
                stateMachine.ClearMovementTarget();
                return;
            }

            Debug.Assert(NavigationFollower != null, "NavigationHelper != null");

            NavigationFollower.UpdatePath(stateMachine, targetPosition);
            NavigationFollower.FollowPath(stateMachine);
        }

        private void PerformAttack()
        {
            Ship.SetAttackTarget(_targetEnemy.WorldWeightedCenter);
            Ship.Shoot();
        }

        public override void Exit(AIShipStateMachine stateMachine)
        {
            base.Exit(stateMachine);
            _targetEnemy = null;
        }

        public override bool CanTransitionTo(string stateName)
        {
            // Can always transition out of attack state
            return true;
        }

        public override string DebugInfo()
        {
            var targetPos = _targetEnemy?.WorldWeightedCenter ?? Vector3.zero;
            var distToTarget = Vector2.Distance(Ship.GetPosition(), targetPos);
            var timeSinceLastAttack = Time.time - _lastAttackTime;
            var cooldownRemaining = Mathf.Max(0, _attackCooldown - timeSinceLastAttack);
            var targetStatus = _targetEnemy?.IsAlive() ?? false ? "Alive" : "Dead";
            return
                $"Dist: {distToTarget:F1} | Range: {_attackRange:F1} | Cooldown: {cooldownRemaining:F2}s | Target: {targetStatus}";
        }
    }
}