using AI.EasyState.States;
using JetBrains.Annotations;

namespace Ships.StateMachines.AIShip.States
{
    public abstract class AIShipState : BaseState<AIShipStateMachine, AIShipState>
    {
        protected Ships.AIShip Ship;
        [CanBeNull] public NavigationFollower NavigationFollower { get; protected set; }

        public override void Enter(AIShipStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            Ship = stateMachine.Controller as Ships.AIShip;
        }

        public override string DebugInfo()
        {
            return StateName;
        }
    }
}