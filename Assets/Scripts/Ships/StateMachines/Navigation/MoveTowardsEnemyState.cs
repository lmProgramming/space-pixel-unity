using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.EasyState.States;
using Core.Ship;
using UnityEngine;

[assembly: InternalsVisibleTo("Game.Editor")]

namespace Ships.StateMachines.Navigation
{
    public class MoveTowardsEnemyState : ShipNavigationState
    {
        private const float PathUpdateInterval = 1.0f;
        private const float WaypointThreshold = 70.0f;
        private int _currentWaypointIndex;
        private float _lastPathUpdateTime;
        private IReadOnlyList<Vector3> _path;
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
                _path = null;
                _currentWaypointIndex = 0;
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

            _path = stateMachine.NavigationService.CalculatePath(Ship.GetPosition(), targetPosition,
                stateMachine.Controller.NavigationSize, Ship, _targetEnemyShip);
            _currentWaypointIndex = 0;
        }

        private void FollowPath(ShipNavigationStateMachine stateMachine)
        {
            if (_path == null || _path.Count == 0) return;

            if (_currentWaypointIndex >= _path.Count)
            {
                stateMachine.SetMovementTarget(_targetEnemyShip.GetPosition());
                return;
            }

            var waypoint = _path[_currentWaypointIndex];
            var distanceToWaypoint = Vector2.Distance(Ship.GetPosition(), waypoint);

            if (distanceToWaypoint < WaypointThreshold)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex < _path.Count)
                    waypoint = _path[_currentWaypointIndex];
            }

            var hasMoreWaypoints = _currentWaypointIndex + 1 < _path.Count;
            var interpolatedWaypoint = waypoint;
            if (hasMoreWaypoints)
                interpolatedWaypoint = ProcessNextWaypoint(waypoint, distanceToWaypoint);

            stateMachine.SetMovementTarget(interpolatedWaypoint);
        }

        private Vector2 ProcessNextWaypoint(Vector2 waypoint,
            float distanceToWaypoint)
        {
            var nextWaypoint = _path[_currentWaypointIndex + 1];

            var distanceToNextWaypoint = Vector2.Distance(Ship.GetPosition(), nextWaypoint);

            if (distanceToNextWaypoint < WaypointThreshold)
            {
                _currentWaypointIndex += 2;
                return _currentWaypointIndex < _path.Count ? _path[_currentWaypointIndex] : nextWaypoint;
            }

            var t = Mathf.Clamp01(WaypointThreshold / distanceToWaypoint);

            return Vector3.Lerp(waypoint, nextWaypoint, t);
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

        public override string DebugInfo()
        {
            var targetPos = _targetEnemyShip?.GetPosition() ?? Vector3.zero;
            var distToTarget = Vector2.Distance(Ship.GetPosition(), targetPos);
            var pathCount = _path?.Count ?? 0;
            var timeSincePathUpdate = Time.time - _lastPathUpdateTime;
            return $"Waypoint: {_currentWaypointIndex}/{pathCount} | Dist: {distToTarget:F1} | Threshold: {_targetDistanceThreshold:F1} | PathAge: {timeSincePathUpdate:F2}s | Target: {(_targetEnemyShip != null ? "Alive" : "Dead")}";
        }

#if UNITY_EDITOR
        internal IReadOnlyList<Vector3> InternalPath => _path;

        internal int InternalCurrentWaypointIndex => _currentWaypointIndex;
#endif
    }
}