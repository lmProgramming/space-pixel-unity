using System.Runtime.CompilerServices;
using AI.EasyState;
using Core.Ships;
using JetBrains.Annotations;
using Ships.StateMachines.AIShip;
using Ships.StateMachines.AIShip.States;
using Ships.Systems.Sensing;
using UnityEngine;
using UnityEngine.Assertions;

[assembly: InternalsVisibleTo("E2E")]

namespace Ships
{
    [RequireComponent(typeof(ShipSensing))]
    [RequireComponent(typeof(AIShipStateMachine))]
    public class AIShip : Ship, IAgent
    {
        [Header("Navigation")]
        [SerializeField] private float speedMultiplier = 1f;

        [SerializeField] private float rotationMultiplier = 1f;
        [SerializeField] private float stopDistance = 1.5f;
        [SerializeField] private float minThrustAlignment = 0.1f;
        [SerializeField] private float avoidanceWeight = 1.2f;
        [SerializeField] private int navigationSize = 1;

        private AIShipStateMachine _aiShipStateMachine;
        private bool _pendingEnginesActive;

        public static float SightRange => 2000f;
        private ShipSensing Sensing { get; set; }

#if UNITY_INCLUDE_TESTS
        internal float InternalStopDistance
        {
            get => stopDistance;
            set => stopDistance = value;
        }
#endif

        protected override void Start()
        {
            base.Start();
            _aiShipStateMachine = GetComponent<AIShipStateMachine>();
            Sensing = GetComponent<ShipSensing>();

            Assert.IsNotNull(Sensing, "Sensing != null");
            Assert.IsNotNull(_aiShipStateMachine, "_aiShipStateMachine != null");

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
            _aiShipStateMachine.Tick(Time.deltaTime);
            PendingForwardInput = 0f;
            PendingHorizontalInput = 0f;
            PendingTurnInput = 0f;
            _pendingEnginesActive = false;

            if (!_aiShipStateMachine.ShouldMove)
                return;

            Debug.Assert(_aiShipStateMachine.Target.HasValue);
            ComputeNavigationInputs(_aiShipStateMachine.Target.Value);
        }

        protected override void ApplyMovementPhysics()
        {
            if (!_pendingEnginesActive)
            {
                MarkEnginesActivity(false);
                return;
            }

            MarkEnginesActivity(ApplyEngineForces(PendingForwardInput, 0, PendingTurnInput, Time.fixedDeltaTime, true));
        }

        private void ComputeNavigationInputs(Vector2 targetPosition)
        {
            if (!CommandModule.Transform)
            {
                Debug.LogWarning("[AIShip] Cannot compute navigation inputs when transform is null");
                return;
            }

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
                PendingTurnInput = rotationMultiplier * (Vector2.SignedAngle(forward, desiredDirection) / 180f);
        }

        private void InitializeStateMachines()
        {
            _aiShipStateMachine.RegisterState(new LookoutState());
            _aiShipStateMachine.RegisterState(new AttackState());
            _aiShipStateMachine.StartStateMachine();
        }

        public void SetAttackTarget(Vector2 targetPosition)
        {
            AttackTargetPosition = targetPosition;
        }

        [CanBeNull]
        public IShip GetClosestEnemyInSight()
        {
            // var result = Sensing.SenseShips(GetPosition(), CommandModule.Transform.up);
            //
            // return !result.HasHit ? null : result.ClosestHit.transform.GetComponent<IModule>()?.Ship;

            return FindClosestEnemy(SightRange);
        }
    }
}