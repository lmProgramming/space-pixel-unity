using AI.EasyState;

namespace Ships.StateMachines.Behaviour
{
    public class BehaviourStateMachine : StateMachine<BehaviourStateMachine, BehaviourState>
    {
        protected override string DefaultState => "Lookout";
    }
}