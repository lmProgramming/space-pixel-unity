using System.Collections.Generic;
using AI.EasyState;

namespace Ships.StateMachines.Behaviour
{
    public class BehaviourStateMachine : StateMachine<BehaviourStateMachine, BehaviourState>
    {
        protected override string DefaultState => "Lookout";

        protected override Dictionary<string, float> CreateWeightedStates()
        {
            return new Dictionary<string, float>
            {
                { "Lookout", 1f }
            };
        }
    }
}