using AI.EasyState.States;

namespace Ships.StateMachines.Navigation
{
    public abstract class ShipNavigationState : BaseState<ShipNavigationStateMachine, ShipNavigationState>
    {
        protected AIShip Ship;

        public override void Enter(ShipNavigationStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            Ship = stateMachine.Controller as AIShip;
        }

        public override string DebugInfo() => StateName;
    }
}