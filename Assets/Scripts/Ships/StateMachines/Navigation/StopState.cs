using AI.EasyState.States;

namespace Ships.StateMachines.Navigation
{
    public class StopState : ShipNavigationState
    {
        public override string StateName => "Stop";

        public override void Enter(ShipNavigationStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            stateMachine.ClearMovementTarget();
        }

        public override bool CanTransitionTo(string stateName)
        {
            return true;
        }
    }
}