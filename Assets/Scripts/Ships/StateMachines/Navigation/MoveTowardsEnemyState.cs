using System;
using System.Collections.Generic;
using AI.EasyState.States;
using Core.Ship;
using UnityEngine;

namespace Ships.StateMachines.Navigation
{
    public class MoveTowardsEnemyState : ShipNavigationState
    {
        private const float PathUpdateInterval = 10.0f;
        private const float WaypointThreshold = 40.0f;
        private float _lastPathUpdateTime;
        private List<Vector3> _path;
        private float _targetDistanceThreshold;
        private IShip _targetEnemyShip;

        public IReadOnlyList<Vector3> Path => _path;
        public int CurrentWaypointIndex { get; private set; }

        public override string StateName => "MoveTowardsEnemy";

        public override void Enter(ShipNavigationStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);

            if (data is EnemyTargetStateData attackData)
            {
                _targetEnemyShip = attackData.TargetEnemy;
                _targetDistanceThreshold = attackData.DistanceThreshold;
                _path = null;
                CurrentWaypointIndex = 0;
            }
            else
            {
                throw new ArgumentException("MoveTowardsEnemyState requires EnemyTargetStateData");
            }

            _lastPathUpdateTime = Time.time - PathUpdateInterval;
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
            var distanceToTarget = Vector2.Distance(Ship.GetPosition(), targetPosition);

            if (distanceToTarget <= _targetDistanceThreshold)
            {
                stateMachine.TransitionToState("Stop");
                return;
            }

            UpdatePath(stateMachine, targetPosition);
            FollowPath(stateMachine);
        }

        private void UpdatePath(ShipNavigationStateMachine stateMachine, Vector3 targetPosition)
        {
            if (Time.time - _lastPathUpdateTime < PathUpdateInterval) return;

            _lastPathUpdateTime = Time.time;

            // For ship size, we'll use a default for now, or we could pass it in. 
            // AIShip has CommandModule which has PixelatedRigidbody, but let's assume a default size if not easily accessible.
            // Actually, SectorService.CalculatePath takes int shipSize.

            _path = stateMachine.SectorService.CalculatePath(Ship.GetPosition(), targetPosition,
                stateMachine.Controller.NavigationSize);
            CurrentWaypointIndex = 0;
        }

        private void FollowPath(ShipNavigationStateMachine stateMachine)
        {
            if (_path == null || _path.Count == 0) return;

            if (CurrentWaypointIndex >= _path.Count)
            {
                stateMachine.SetMovementTarget(_targetEnemyShip.GetPosition());
                return;
            }

            var waypoint = _path[CurrentWaypointIndex];
            var distanceToWaypoint = Vector2.Distance(Ship.GetPosition(), waypoint);

            if (distanceToWaypoint < WaypointThreshold)
            {
                CurrentWaypointIndex++;
                if (CurrentWaypointIndex < _path.Count) waypoint = _path[CurrentWaypointIndex];
            }

            stateMachine.SetMovementTarget(waypoint);
        }

        public override void Exit(ShipNavigationStateMachine stateMachine)
        {
            _path = null;
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