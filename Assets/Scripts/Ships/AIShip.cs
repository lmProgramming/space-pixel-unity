using AI.EasyState;
using Core.Ship;
using Ships.AIStates;
using UnityEngine;

namespace Ships
{
    public class AIShip : Ship, IAgent
    {
        private StateMachine _stateMachine;

        public float SightRange => 200f;

        protected override void Start()
        {
            base.Start();
            _stateMachine = GetComponent<StateMachine>();

            InitializeStateMachine();
        }

        public Transform Transform => transform;

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

        public IShip GetClosestEnemyInSight()
        {
            return FindClosestEnemy(SightRange);
        }
    }
}