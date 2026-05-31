using AI.EasyState.States;

namespace Ships.StateMachines.Behaviour
{
    public abstract class BehaviourState : BaseState<BehaviourStateMachine, BehaviourState>
    {
        protected AIShip Ship;

        public override void Enter(BehaviourStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            Ship = stateMachine.Controller as AIShip;
        }

        public override string DebugInfo() => StateName;
    }
}