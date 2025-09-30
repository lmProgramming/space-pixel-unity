using Random = UnityEngine.Random;

namespace AI.EasyState.States
{
    public class LookoutState : BaseState
    {
        private const float StateChangeInterval = 3f;
        private const float StateChangeVariance = 2f;
        private const float StateChangeProbability = 0.1f;

        private float _statePotentialChangeTime;

        public override string StateName => "Lookout";

        public override void Enter(StateMachine stateMachine)
        {
            base.Enter(stateMachine);
            _statePotentialChangeTime = GetStatePotentialChangeTime();
        }

        public override void Update(StateMachine stateMachine, float deltaTime)
        {
            base.Update(stateMachine, deltaTime);

            var enemyShip = stateMachine.ShipController.GetClosestEnemyInSight();

            if (enemyShip)
            {
                var attackData = new AttackStateData(enemyShip, stateMachine.ShipController.SightRange, 0.5f);
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
    }
}