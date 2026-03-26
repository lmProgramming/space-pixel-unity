using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Core.Gameplay.Combat;
using Core.Gameplay.EasyTeam;
using Core.Pixelation;
using Core.Services;
using Core.Ship;
using Cysharp.Threading.Tasks;
using Events.Ship;
using Gameplay.EasyTeam;
using LMPro;
using LMPro.Graph;
using Ships.Internal;
using Ships.Modules;
using UnityEngine;
using UnityEngine.Assertions;
using Zenject;
using ZLinq;

[assembly: InternalsVisibleTo("Game.Ships.Tests")]

namespace Ships
{
    [RequireComponent(typeof(ResourceManager))]
    [DisallowMultipleComponent]
    public class Ship : MonoBehaviour, IShip
    {
        private const float UpdateResourcesTimer = 0.1f;

        [SerializeField]
        private Team team;

        [Header("SAS")]
        [SerializeField] private float sasTurnReleaseThreshold = 0.05f;

        [SerializeField] private float sasHeadingDeadZoneDegrees = 0.3f;
        [SerializeField] private float sasHeadingGain = 0.04f;
        [SerializeField] private float sasAngularVelocityGain = 0.03f;
        [SerializeField] private float sasMaxTurnInput = 2f;
        [SerializeField] private float sasForwardCompensationStrength = 1f;
        [SerializeField] private float sasForwardCompensationMaxTurnInput = 1.5f;

        private readonly List<Module> _allModulesCache = new();

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modulesDictionary = new(() => new List<Module>());

        private BiCohesionGraph<IModule> _biCohesionGraph;

        [Inject]
        private IMapInfo _mapInfo;

        private IModuleConnectionFactory _moduleConnectionFactory;

        private Action<IPixelated> _onCommandModuleNoPixelsLeft;
        private float _sasDesiredHeadingDegrees;
        private bool _sasHasDesiredHeading;
        private bool _sasWasTurning;

        [Inject]
        private ShipInitializeModulesEventChannel _shipInitializeModulesEventChannel;

        [Inject]
        protected IShipService ShipService;

        internal IModuleConnectionFactory ModuleConnectionFactoryForTesting
        {
            set => _moduleConnectionFactory = value;
        }

        private IReadOnlyList<Module> AllModules => _allModulesCache;

        public List<IWeapon> Weapons =>
            _modulesDictionary[ModuleType.Weapon].AsValueEnumerable().Cast<IWeapon>().ToList();

        public List<Engine> Engines =>
            _modulesDictionary[ModuleType.Engine].AsValueEnumerable().Cast<Engine>().ToList();

        public ResourceManager ResourceManager { get; private set; }

        public int CrewMissingCount =>
            ModuleGraph.GetAllNodes().AsValueEnumerable().Sum(module => module.CrewMissingCount);

        private void Awake()
        {
            CommandModule ??= GetComponentInChildren<Command>();
            ResourceManager = GetComponent<ResourceManager>();

            _moduleConnectionFactory =
                GetComponent<IModuleConnectionFactory>();

            Assert.IsNotNull(CommandModule, "CommandModule != null");
            Assert.IsNotNull(_moduleConnectionFactory, "_moduleConnectionFactory != null");
        }

        protected virtual void Start()
        {
            InitializeModules();
            CaptureSasHeadingFromCurrent();

            UpdateResourcesLoop().Forget();
        }

        private void Update()
        {
            Move();

            HandleWeapons();
            ResourceManager.UpdateEnergy();
        }

        private void OnEnable()
        {
            ShipService.RegisterShip(this);
        }

        private void OnDisable()
        {
            ShipService.UnregisterShip(this);
        }

        private void OnDestroy()
        {
            if (_biCohesionGraph != null)
                _biCohesionGraph.OnNodesRemovedDueToUnreachability -= HandleUnreachableModules;

            if (CommandModule != null && _onCommandModuleNoPixelsLeft != null)
                CommandModule.PixelatedRigidbody.OnNoPixelsLeft -= _onCommandModuleNoPixelsLeft;
        }

        public float GeneralEfficiency => Math.Max(0.01f, ResourceManager.EnergyEfficiency);

        public Graph<IModule> ModuleGraph => _biCohesionGraph;

        public Vector2 AttackTargetPosition { get; protected set; }

        public float CaptainMultiplier => CommandModule.GetCrewEfficiency();

        public ITeam Team => team;

        public IModule CommandModule { get; private set; }

        public Collider2D[] OwnColliders { get; private set; } = Array.Empty<Collider2D>();

        public Vector2 GetPosition()
        {
            var rb = CommandModule.PixelatedRigidbody;
            return rb.LocalToWorldPoint(rb.WeightedCenter);
        }

        public void OnModuleDestroyed(IModule module)
        {
            if (module == null) return;

            if (module == CommandModule) DestroyShip();

            Debug.Log($"[Ship] Module destroyed: {module.Transform.name}", module.Transform);

            _biCohesionGraph.RemoveNode(module);
            RecacheModulesDictionary();
        }

        public void ManualAddModule(IModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            module.Transform.SetParent(transform);
            InitializeModules();
        }

        public void ManualRemoveModule(IModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            module.Transform.SetParent(null);
            InitializeModules();
        }

        private void DestroyShip()
        {
            Debug.Log($"[Ship] Command module destroyed. Destroying ship: {gameObject.name}", gameObject);
            Destroy(gameObject);
        }

        private async UniTaskVoid UpdateResourcesLoop()
        {
            var updateResourcesTimer = new SimpleTimer(UpdateResourcesTimer);

            var token = this.GetCancellationTokenOnDestroy();

            while (!token.IsCancellationRequested)
            {
                await updateResourcesTimer.Wait(cancellationToken: token);
                ResourceManager.Recalculate(AllModules);
            }
        }

        private void HandleUnreachableModules(List<IModule> unreachableModules)
        {
            Debug.Log(
                $"[Ship] HandleUnreachableModules called with {unreachableModules.Count} modules: [{string.Join(", ", unreachableModules.AsValueEnumerable().Select(m => m?.Transform?.name ?? "null"))}]");

            foreach (var module in unreachableModules.AsValueEnumerable().Where(module =>
                         module?.Transform != null &&
                         module.Transform.parent != _mapInfo.MapTransform))
            {
                Debug.Log($"[Ship] Deparenting unreachable module: {module.Transform.name}", module.Transform);
                module.Transform.SetParent(_mapInfo.MapTransform);
                module.Transform.gameObject.layer = LayerMask.NameToLayer("Default");

                if (module is Module concreteModule) concreteModule.OnShipConnectionLost();
            }

            RecacheModulesDictionary();
        }

        private void RecacheModulesDictionary()
        {
            _modulesDictionary.Clear();
            _allModulesCache.Clear();

            foreach (var module in ModuleGraph.GetAllNodes())
            {
                var mod = module as Module;
                _modulesDictionary[module.Type].Add(mod);
                _allModulesCache.Add(mod);
            }

            OwnColliders = GetComponentsInChildren<Collider2D>();

            ResourceManager.Recalculate(_allModulesCache);
        }

        internal void InitializeModules()
        {
            if (CommandModule != null && _onCommandModuleNoPixelsLeft != null)
                CommandModule.PixelatedRigidbody.OnNoPixelsLeft -= _onCommandModuleNoPixelsLeft;

            CommandModule = GetComponentInChildren<Command>();
            if (CommandModule == null)
                throw new UnityException("[Ship] ReinitializeModules: No Command module found on ship!");

            if (_biCohesionGraph != null)
                _biCohesionGraph.OnNodesRemovedDueToUnreachability -= HandleUnreachableModules;

            _biCohesionGraph = new BiCohesionGraph<IModule>(CommandModule);
            _biCohesionGraph.OnNodesRemovedDueToUnreachability += HandleUnreachableModules;

            _onCommandModuleNoPixelsLeft = _ => Destroy(gameObject);
            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _onCommandModuleNoPixelsLeft;

            _moduleConnectionFactory.ConnectModules(this);

            _shipInitializeModulesEventChannel.Raise();

            RecacheModulesDictionary();
        }

        protected virtual void Move()
        {
        }

        protected virtual void HandleWeapons()
        {
        }

        public void Shoot()
        {
            foreach (var weapon in Weapons)
                weapon.Shoot();
        }

        protected void StopShooting()
        {
            foreach (var weapon in Weapons)
                weapon.StopShooting();
        }

        public void AssignCrewBySkill(IEnumerable<CrewMember> crew)
        {
            var crewList = crew.ToList();

            foreach (var module in ModuleGraph.GetAllNodes())
            {
                module.FillCrewBySkill(crewList, out var remainingCrew);
                crewList = remainingCrew.ToList();
            }
        }

        protected void MarkEnginesActivity(bool active)
        {
            foreach (var engine in Engines)
                engine.SetActive(active);
        }

        protected bool ApplyEngineForces(float forwardInput, float turnInput, float deltaTime,
            bool sasEnabled = false)
        {
            var selfRigidbody = CommandModule.PixelatedRigidbody?.Rigidbody;
            Assert.IsNotNull(selfRigidbody, "CommandModule.PixelatedRigidbody.Rigidbody != null");

            var finalTurnInput = GetFinalTurnInput(turnInput, forwardInput, selfRigidbody, sasEnabled);

            var engines = Engines;
            if (engines.Count == 0) return false;

            var forward = CommandModule.Transform.up;
            var centerOfMass = selfRigidbody.worldCenterOfMass;
            var maxLeverArm = GetMaxLeverArmLength(engines, centerOfMass);

            var anyForceApplied = false;

            foreach (var engine in engines)
            {
                if (engine.MaxThrust <= 0f)
                {
                    engine.RotateThrusterTowards(0f, deltaTime);
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                var desiredDirection = GetDesiredEngineDirection(
                    forward,
                    centerOfMass,
                    maxLeverArm,
                    engine,
                    forwardInput,
                    finalTurnInput);

                if (desiredDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    engine.RotateThrusterTowards(0f, deltaTime);
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                var desiredAngle = Vector2.SignedAngle(engine.Transform.up, desiredDirection.normalized);
                engine.RotateThrusterTowards(desiredAngle, deltaTime);

                var thrust = Mathf.Clamp01(desiredDirection.magnitude) * engine.MaxThrust;
                engine.SetCurrentThrust(thrust);
                var force = engine.WorldThrustDirection * thrust;

                selfRigidbody.AddForceAtPosition(force, engine.WorldThrustPoint);
                anyForceApplied |= force.sqrMagnitude > Mathf.Epsilon;
            }

            return anyForceApplied;
        }

        private float GetFinalTurnInput(float requestedTurnInput, float forwardInput, Rigidbody2D selfRigidbody,
            bool sasEnabled)
        {
            if (!sasEnabled) return requestedTurnInput;

            UpdateSasDesiredHeadingOnTurnRelease(requestedTurnInput);

            var headingHoldTurnInput = GetSasHeadingHoldTurnInput(requestedTurnInput, selfRigidbody);
            if (Mathf.Abs(requestedTurnInput) > sasTurnReleaseThreshold || Mathf.Abs(forwardInput) <= Mathf.Epsilon)
                return headingHoldTurnInput;

            var forwardCompensation = CalculateForwardThrustCompensationTurnInput(forwardInput);
            var withForwardCompensation = headingHoldTurnInput +
                                          forwardCompensation * sasForwardCompensationStrength;

            return Mathf.Clamp(withForwardCompensation, -sasMaxTurnInput, sasMaxTurnInput);
        }

        private void UpdateSasDesiredHeadingOnTurnRelease(float requestedTurnInput)
        {
            CaptureSasHeadingFromCurrentIfNeeded();

            var isTurning = Mathf.Abs(requestedTurnInput) > sasTurnReleaseThreshold;
            if (_sasWasTurning && !isTurning) CaptureSasHeadingFromCurrent();

            _sasWasTurning = isTurning;
        }

        private float GetSasHeadingHoldTurnInput(float requestedTurnInput, Rigidbody2D selfRigidbody)
        {
            if (Mathf.Abs(requestedTurnInput) > sasTurnReleaseThreshold) return requestedTurnInput;

            CaptureSasHeadingFromCurrentIfNeeded();

            var headingError = Mathf.DeltaAngle(GetCurrentHeadingDegrees(), _sasDesiredHeadingDegrees);
            if (Mathf.Abs(headingError) < sasHeadingDeadZoneDegrees)
                headingError = 0f;

            var angularVelocityDamping = -selfRigidbody.angularVelocity * sasAngularVelocityGain;
            var turnCorrection = headingError * sasHeadingGain + angularVelocityDamping;

            return Mathf.Clamp(turnCorrection, -sasMaxTurnInput, sasMaxTurnInput);
        }

        private float CalculateForwardThrustCompensationTurnInput(float forwardInput)
        {
            var selfRigidbody = CommandModule.PixelatedRigidbody?.Rigidbody;
            Assert.IsNotNull(selfRigidbody, "CommandModule.PixelatedRigidbody.Rigidbody != null");

            var engines = Engines;
            if (engines.Count == 0) return 0f;

            var forward = CommandModule.Transform.up;
            var centerOfMass = selfRigidbody.worldCenterOfMass;
            var maxLeverArm = GetMaxLeverArmLength(engines, centerOfMass);

            const float sampleTurnInput = 0.2f;

            var baselineTorque = EstimateNetTorqueForTurnInput(engines, forward, centerOfMass, maxLeverArm,
                forwardInput, 0f);
            if (Mathf.Abs(baselineTorque) <= 0.0001f) return 0f;

            var positiveSampleTorque = EstimateNetTorqueForTurnInput(engines, forward, centerOfMass, maxLeverArm,
                forwardInput, sampleTurnInput);
            var negativeSampleTorque = EstimateNetTorqueForTurnInput(engines, forward, centerOfMass, maxLeverArm,
                forwardInput, -sampleTurnInput);

            var torqueSlope = (positiveSampleTorque - negativeSampleTorque) / (sampleTurnInput * 2f);
            if (Mathf.Abs(torqueSlope) <= 0.0001f) return 0f;

            var compensation = -baselineTorque / torqueSlope;
            return Mathf.Clamp(compensation, -sasForwardCompensationMaxTurnInput, sasForwardCompensationMaxTurnInput);
        }

        private static float EstimateNetTorqueForTurnInput(IReadOnlyList<Engine> engines, Vector2 shipForward,
            Vector2 centerOfMass, float maxLeverArm, float forwardInput, float turnInput)
        {
            var netTorque = 0f;

            foreach (var engine in engines)
            {
                if (engine.MaxThrust <= 0f) continue;

                var desiredDirection = GetDesiredEngineDirection(shipForward, centerOfMass, maxLeverArm, engine,
                    forwardInput, turnInput);

                if (desiredDirection.sqrMagnitude <= Mathf.Epsilon) continue;

                var thrust = Mathf.Clamp01(desiredDirection.magnitude) * engine.MaxThrust;
                var force = desiredDirection.normalized * thrust;
                var lever = engine.WorldThrustPoint - centerOfMass;
                netTorque += lever.x * force.y - lever.y * force.x;
            }

            return netTorque;
        }

        private float GetCurrentHeadingDegrees()
        {
            return CommandModule.Transform.eulerAngles.z;
        }

        private void CaptureSasHeadingFromCurrentIfNeeded()
        {
            if (!_sasHasDesiredHeading)
                CaptureSasHeadingFromCurrent();
        }

        private void CaptureSasHeadingFromCurrent()
        {
            _sasDesiredHeadingDegrees = GetCurrentHeadingDegrees();
            _sasHasDesiredHeading = true;
        }

        private static float GetMaxLeverArmLength(IEnumerable<Engine> engines, Vector2 centerOfMass)
        {
            var maxLeverArm = engines.AsValueEnumerable().Select(engine => engine.WorldThrustPoint - centerOfMass)
                .Aggregate(0f, (current, lever) => Mathf.Max(current, lever.magnitude));

            return Mathf.Max(maxLeverArm, 0.01f);
        }

        private static Vector2 GetDesiredEngineDirection(Vector2 shipForward, Vector2 centerOfMass, float maxLeverArm,
            Engine engine, float forwardInput, float turnInput)
        {
            var lever = engine.WorldThrustPoint - centerOfMass;
            var rotationalDirection = new Vector2(-lever.y, lever.x) / maxLeverArm;
            return shipForward * forwardInput + rotationalDirection * turnInput;
        }

        protected IShip FindClosestEnemy(float maxRange = float.MaxValue)
        {
            var allShips = ShipService.GetEnemyShipsOf(Team);
            IShip closestEnemy = null;
            var closestDistance = maxRange;

            foreach (var ship in allShips)
            {
                var distance = Vector2.Distance(GetPosition(), ship.GetPosition());
                if (!(distance < closestDistance)) continue;

                closestDistance = distance;
                closestEnemy = ship;
            }

            return closestEnemy;
        }
    }
}