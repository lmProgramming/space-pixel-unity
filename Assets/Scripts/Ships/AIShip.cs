using AI.StateMachine;
using AI.StateMachine.States;

namespace Ships
{
    public class AIShip : Ship
    {
        private StateMachine _stateMachine;

        protected override void Start()
        {
            base.Start();
            _stateMachine = GetComponent<StateMachine>();

            InitializeStateMachine();
        }

        private void InitializeStateMachine()
        {
            _stateMachine.RegisterState(new IdleState());
            _stateMachine.StartStateMachine();
        }
    }
}