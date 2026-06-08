using AI.EasyState.States;
using Ships.StateMachines.AIShip.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Ships.StateMachines.AIShip.States
{
    public class LookoutState : AIShipState
    {
        private const float StateChangeInterval = 3f;
        private const float StateChangeVariance = 2f;
        private const float StateChangeProbability = 0.1f;

        private float _statePotentialChangeTime;

        public override string StateName => "Lookout";

        public override void Enter(AIShipStateMachine stateMachine, IStateData data)
        {
            base.Enter(stateMachine, data);
            _statePotentialChangeTime = GetStatePotentialChangeTime();
        }

        public override void Update(AIShipStateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);

            var enemyShip = Ship.GetClosestEnemyInSight();

            if (enemyShip != null)
            {
                var attackData =
                    new AttackStateData(enemyShip.CommandModule.PixelatedRigidbody, Ships.AIShip.SightRange, 0.5f);
                stateMachine.TransitionToState("Attack", attackData);
                return;
            }

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

        public override string DebugInfo()
        {
            var timeUntilChange = _statePotentialChangeTime - TimeInState;
            var enemy = Ship.GetClosestEnemyInSight();
            var enemyStatus = enemy != null
                ? $"Enemy found (dist: {Vector2.Distance(Ship.GetPosition(), enemy.GetPosition()):F1})"
                : "No enemy in sight";
            return $"Time in state: {TimeInState:F2}s | Until change: {timeUntilChange:F2}s | {enemyStatus}";
        }
    }
}