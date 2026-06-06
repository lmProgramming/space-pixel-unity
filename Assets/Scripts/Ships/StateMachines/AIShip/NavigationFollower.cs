using System.Collections.Generic;
using Core.Pixelation;
using UnityEngine;

namespace Ships.StateMachines.AIShip
{
    public class NavigationFollower
    {
        private const float PathUpdateInterval = 1.0f;
        private const float SectorToWaypointThresholdMultiplier = 0.7f;
        private readonly float _waypointThreshold;
        public readonly Ship Ship;
        private int _currentWaypointIndex;
        private float _lastPathUpdateTime;
        private IReadOnlyList<Vector3> _path;
        private IPixelatedRigidbody _target;
        private float _targetDistanceThreshold;

        public NavigationFollower(Ship ship, float sectorSize)
        {
            Ship = ship;
            _lastPathUpdateTime = Time.time - PathUpdateInterval;
            _waypointThreshold = sectorSize * SectorToWaypointThresholdMultiplier;
        }

        public void UpdatePath(AIShipStateMachine stateMachine, Vector3 targetPosition)
        {
            if (Time.time - _lastPathUpdateTime < PathUpdateInterval) return;

            _lastPathUpdateTime = Time.time;

            _path = stateMachine.NavigationService.CalculatePath(Ship.GetPosition(), targetPosition,
                stateMachine.Controller.NavigationSize, Ship, _target);
            _currentWaypointIndex = 0;
        }

        public void FollowPath(AIShipStateMachine stateMachine)
        {
            if (_path == null || _path.Count == 0) return;

            if (_currentWaypointIndex >= _path.Count)
            {
                stateMachine.SetMovementTarget(_target.WorldWeightedCenter);
                return;
            }

            var waypoint = _path[_currentWaypointIndex];
            var distanceToWaypoint = Vector2.Distance(Ship.GetPosition(), waypoint);

            if (distanceToWaypoint < _waypointThreshold)
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

            if (distanceToNextWaypoint < _waypointThreshold)
            {
                _currentWaypointIndex += 2;
                return _currentWaypointIndex < _path.Count ? _path[_currentWaypointIndex] : nextWaypoint;
            }

            var t = Mathf.Clamp01(_waypointThreshold / distanceToWaypoint);

            return Vector3.Lerp(waypoint, nextWaypoint, t);
        }

        public NavigationFollower WithTarget(IPixelatedRigidbody target)
        {
            _target = target;
            return this;
        }

#if UNITY_EDITOR
        // ReSharper disable ConvertToAutoPropertyWithPrivateSetter
        internal int InternalCurrentWaypointIndex => _currentWaypointIndex;
        internal IReadOnlyList<Vector3> InternalPath => _path;
        // ReSharper restore ConvertToAutoPropertyWithPrivateSetter
#endif
    }
}