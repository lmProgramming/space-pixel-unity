using AI.EasyState;
using AI.EasyState.States;

namespace Ships.AIStates
{
    public abstract class AIShipState : BaseState
    {
        protected AIShip Ship;

        public override void Enter(StateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            Ship = stateMachine.Controller as AIShip;
        }
    }
}