using AI.EasyState;
using UnityEngine;

namespace Ships.StateMachines.Navigation
{
    public sealed class ShipNavigationStateMachine : StateMachine<ShipNavigationStateMachine, ShipNavigationState>
    {
        public bool ShouldMove { get; private set; }
        public Vector2 Target { get; private set; }

        protected override string DefaultState => "MoveTowardsEnemy";

        public void SetMovementTarget(Vector2 target)
        {
            Target = target;
            ShouldMove = true;
        }

        public void ClearMovementTarget()
        {
            ShouldMove = false;
        }

        public void ResetMovementTarget()
        {
            ShouldMove = false;
            Target = Vector2.zero;
        }
    }
}