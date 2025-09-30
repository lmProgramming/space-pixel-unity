using AI.EasyState;
using AI.EasyState.States;
using UnityEngine;

namespace Ships
{
    public class AIShip : Ship
    {
        private StateMachine _stateMachine;

        public float SightRange => 200f;

        protected override void Start()
        {
            base.Start();
            _stateMachine = GetComponent<StateMachine>();

            InitializeStateMachine();
        }

        private void InitializeStateMachine()
        {
            _stateMachine.RegisterState(new LookoutState());
            _stateMachine.RegisterState(new AttackState());
            _stateMachine.StartStateMachine();
        }

        public void SetAttackTarget(Vector2 targetPosition)
        {
            AttackTargetPosition = targetPosition;
        }

        public Ship GetClosestEnemyInSight()
        {
            return FindFirstObjectByType<PlayerShip>();
        }
    }
}