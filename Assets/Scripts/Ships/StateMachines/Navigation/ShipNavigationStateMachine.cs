using AI.EasyState;
using Core.Services;
using UnityEngine;
using Zenject;

namespace Ships.StateMachines.Navigation
{
    public sealed class ShipNavigationStateMachine : StateMachine<ShipNavigationStateMachine, ShipNavigationState>
    {
        [Inject] public INavigationService NavigationService { get; private set; }
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