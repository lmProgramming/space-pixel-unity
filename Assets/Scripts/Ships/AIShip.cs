using AI.EasyState;
using Core.Ship;
using Ships.StateMachines.Behaviour;
using Ships.StateMachines.Navigation;
using UnityEngine;

namespace Ships
{
    [RequireComponent(typeof(ShipSensing))]
    [RequireComponent(typeof(ShipNavigationStateMachine))]
    [RequireComponent(typeof(BehaviourStateMachine))]
    public class AIShip : Ship, IAgent
    {
        [Header("Navigation")]
        [SerializeField] private float speedMultiplier = 1f;

        [SerializeField] private float rotationMultiplier = 1f;
        [SerializeField] private float stopDistance = 1.5f;
        [SerializeField] private float minThrustAlignment = 0.1f;
        [SerializeField] private float avoidanceWeight = 1.2f;
        [SerializeField] private int navigationSize = 1;

        private BehaviourStateMachine _behaviourStateMachine;
        private bool _pendingEnginesActive;
        private ShipNavigationStateMachine _navigationStateMachine;

        public static float SightRange => 200f;
        private ShipSensing Sensing { get; set; }

        protected override void Start()
        {
            base.Start();
            _behaviourStateMachine = GetComponent<BehaviourStateMachine>();
            _navigationStateMachine = GetComponent<ShipNavigationStateMachine>();
            Sensing = GetComponent<ShipSensing>();

            _navigationStateMachine.UseManualUpdate = true;

            InitializeStateMachines();
        }

        public int NavigationSize => navigationSize;

        public Transform Transform => transform;

        public void SetNavigationSize(int size)
        {
            navigationSize = size;
        }

        protected override void ReadMovementInput()
        {
            _navigationStateMachine.Tick(Time.deltaTime);
            PendingForwardInput = 0f;
            PendingTurnInput = 0f;
            _pendingEnginesActive = false;

            if (!_navigationStateMachine.ShouldMove)
                return;

            ComputeNavigationInputs(_navigationStateMachine.Target);
        }

        protected override void ApplyMovementPhysics()
        {
            if (!_pendingEnginesActive)
            {
                MarkEnginesActivity(false);
                return;
            }

            MarkEnginesActivity(ApplyEngineForces(PendingForwardInput, PendingTurnInput, Time.fixedDeltaTime, true));
        }

        private void ComputeNavigationInputs(Vector2 targetPosition)
        {
            var position = (Vector2)CommandModule.Transform.position;
            var toTarget = targetPosition - position;
            var distance = toTarget.magnitude;

            if (distance <= stopDistance)
                return;

            var desired = distance > Mathf.Epsilon ? toTarget / distance : Vector2.zero;
            var forward = (Vector2)CommandModule.Transform.up;

            if (Sensing)
            {
                var obstacleSense = Sensing.SenseObstacles(position, forward);
                if (obstacleSense.HasHit)
                {
                    var avoidance = obstacleSense.Avoidance.sqrMagnitude > 0f
                        ? obstacleSense.Avoidance.normalized * avoidanceWeight
                        : Vector2.zero;
                    desired = (desired + avoidance).normalized;
                }
            }

            SetMovementInputsFromDesired(desired, forward);
            _pendingEnginesActive = true;
        }

        private void SetMovementInputsFromDesired(Vector2 desiredDirection, Vector2 forward)
        {
            PendingForwardInput = 0f;
            if (desiredDirection.sqrMagnitude > 0f)
            {
                var alignment = Mathf.Clamp01(Vector2.Dot(forward.normalized, desiredDirection.normalized));
                if (alignment >= minThrustAlignment)
                    PendingForwardInput = speedMultiplier * alignment;
            }

            PendingTurnInput = 0f;
            if (desiredDirection.sqrMagnitude > 0f)
                PendingTurnInput = rotationMultiplier * (-Vector2.SignedAngle(forward, desiredDirection) / 180f);
        }

        private void InitializeStateMachines()
        {
            _behaviourStateMachine.RegisterState(new LookoutState());
            _behaviourStateMachine.RegisterState(new AttackState());
            _behaviourStateMachine.StartStateMachine();

            _navigationStateMachine.RegisterState(new MoveTowardsEnemyState());
            _navigationStateMachine.RegisterState(new StopState());

            _navigationStateMachine.StartStateMachine("MoveTowardsEnemy",
                new EnemyTargetStateData(ShipService.GetClosestEnemyShipOf(Team, GetPosition())));
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
