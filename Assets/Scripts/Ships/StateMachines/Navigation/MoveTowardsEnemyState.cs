using System;
using AI.EasyState.States;
using Core.Ship;
using UnityEngine;

namespace Ships.StateMachines.Navigation
{
    public class MoveTowardsEnemyState : ShipNavigationState
    {
        private float _targetDistanceThreshold;
        private IShip _targetEnemyShip;

        public override string StateName => "MoveTowardsEnemy";

        public override void Enter(ShipNavigationStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);

            if (data is EnemyTargetStateData attackData)
            {
                _targetEnemyShip = attackData.TargetEnemy;
                _targetDistanceThreshold = attackData.DistanceThreshold;
            }
            else
            {
                throw new ArgumentException("MoveTowardsEnemyState requires EnemyTargetStateData");
            }
        }

        public override void Update(ShipNavigationStateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);

            if (_targetEnemyShip == null)
            {
                stateMachine.ClearMovementTarget();
                stateMachine.TransitionToState("Stop");
                return;
            }

            var targetPosition = _targetEnemyShip.GetPosition();
            stateMachine.SetMovementTarget(targetPosition);

            var distanceToTarget = Vector2.Distance(stateMachine.transform.position, targetPosition);

            if (distanceToTarget <= _targetDistanceThreshold) stateMachine.TransitionToState("Stop");
        }

        public override void Exit(ShipNavigationStateMachine stateMachine)
        {
            base.Exit(stateMachine);
            stateMachine.ClearMovementTarget();
            _targetEnemyShip = null;
        }

        public override bool CanTransitionTo(string stateName)
        {
            return true;
        }
    }
}