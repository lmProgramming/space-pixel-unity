using AI.EasyState;
using AI.EasyState.States;
using Random = UnityEngine.Random;

namespace Ships.AIStates
{
    public class MovingState : AIShipState
    {
        private const float StateChangeInterval = 3f;
        private const float StateChangeVariance = 2f;
        private const float StateChangeProbability = 0.1f;

        private float _statePotentialChangeTime;

        public override string StateName => "Moving";

        public override void Enter(StateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            _statePotentialChangeTime = GetStatePotentialChangeTime();
        }

        public override void Update(StateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);

            if (TimeInState < _statePotentialChangeTime) return;

            if (Random.value < StateChangeProbability)
            {
                stateMachine.TransitionToNextState();
                return;
            }

            _statePotentialChangeTime = TimeInState + GetStatePotentialChangeTime();
        }

        private static float GetStatePotentialChangeTime()
        {
            return StateChangeInterval + Random.Range(-StateChangeVariance, StateChangeVariance);
        }

        public override bool CanTransitionTo(string _)
        {
            return true;
        }
    }
}