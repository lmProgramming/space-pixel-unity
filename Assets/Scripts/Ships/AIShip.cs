using AI.EasyState;
using Core.Ship;
using Ships.StateMachines.Behaviour;
using Ships.StateMachines.Navigation;
using UnityEngine;
using ZLinq;

namespace Ships
{
    [RequireComponent(typeof(ShipSensing))]
    [RequireComponent(typeof(ShipNavigationStateMachine))]
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

        protected override void Move()
        {
            _navigationStateMachine.Tick(Time.deltaTime);

            if (_navigationStateMachine.ShouldMove)
                NavigateTowards(_navigationStateMachine.Target);
        }

        private void NavigateTowards(Vector2 targetPosition)
        {
            var selfRigidbody = CommandModule.PixelatedRigidbody?.Rigidbody;

            var position = (Vector2)CommandModule.Transform.position;
            var toTarget = targetPosition - position;
            var distance = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                MarkEnginesActivity(false);
                return;
            }

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

            ApplyMovement(selfRigidbody, forward, desired);
        }

        private void ApplyMovement(Rigidbody2D selfRigidbody, Vector2 forward, Vector2 desiredDirection)
        {
            var availableThrust = Engines.AsValueEnumerable().Sum(e => e.maxThrust);
            if (availableThrust <= 0f) return;

            var alignment = Mathf.Clamp01(Vector2.Dot(forward, desiredDirection));
            if (alignment >= minThrustAlignment)
            {
                var acceleration = speedMultiplier * availableThrust * alignment;
                selfRigidbody.AddForce(forward * acceleration);
            }

            if (desiredDirection.sqrMagnitude > 0f)
            {
                var turn = -Vector2.SignedAngle(forward, desiredDirection);
                var torque = rotationMultiplier * availableThrust * Mathf.Sign(turn) * Mathf.Abs(turn) / 180f;
                selfRigidbody.AddTorque(torque);
            }

            MarkEnginesActivity(true);
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