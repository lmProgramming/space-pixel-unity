namespace AI.EasyState.States
{
    public interface IState
    {
        string StateName { get; }
        public bool OverridableByForce { get; }
        void Enter(StateMachine stateMachine);
        void Enter(StateMachine stateMachine, IStateData data);
        void Update(StateMachine stateMachine, float deltaTime);
        void Exit(StateMachine stateMachine);
        bool CanTransitionTo(string stateName);
    }
}