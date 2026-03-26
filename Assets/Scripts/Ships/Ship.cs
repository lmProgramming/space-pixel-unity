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

        [Header("Control Allocator")]
        [SerializeField] private int allocatorIterations = 14;

        [SerializeField] private float allocatorForceWeight = 1f;
        [SerializeField] private float allocatorTorqueWeight = 0.4f;
        [SerializeField] private float allocatorRegularization = 0.02f;

        private readonly List<Module> _allModulesCache = new();

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modulesDictionary = new(() => new List<Module>());

        private BiCohesionGraph<IModule> _biCohesionGraph;

        private ControlAllocator _controlAllocator;

        private EngineDirectionSolver _engineDirectionSolver;

        [Inject]
        private IMapInfo _mapInfo;

        private IModuleConnectionFactory _moduleConnectionFactory;

        private Action<IPixelated> _onCommandModuleNoPixelsLeft;

        private SasTurnInputResolver _sasTurnInputResolver;

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
            _moduleConnectionFactory = GetComponent<IModuleConnectionFactory>();
            _engineDirectionSolver = new EngineDirectionSolver();
            _controlAllocator = new ControlAllocator();
            _sasTurnInputResolver = new SasTurnInputResolver(_engineDirectionSolver);

            Assert.IsNotNull(CommandModule, "CommandModule != null");
            Assert.IsNotNull(ResourceManager, "ResourceManager != null");
            Assert.IsNotNull(_moduleConnectionFactory, "_moduleConnectionFactory != null");
            Assert.IsNotNull(_engineDirectionSolver, "_engineDirectionSolver != null");
            Assert.IsNotNull(_controlAllocator, "_controlAllocator != null");
            Assert.IsNotNull(_sasTurnInputResolver, "_sasTurnInputResolver != null");
        }

        protected virtual void Start()
        {
            InitializeModules();
            _sasTurnInputResolver.CaptureDesiredHeading(GetCurrentHeadingDegrees());

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

            var engines = Engines;
            if (engines.Count == 0) return false;

            var forward = CommandModule.Transform.up;
            var centerOfMass = selfRigidbody.worldCenterOfMass;
            var maxLeverArm = _engineDirectionSolver.GetMaxLeverArmLength(engines, centerOfMass);
            var finalTurnInput = sasEnabled
                ? _sasTurnInputResolver.ResolveTurnInput(turnInput, forwardInput, selfRigidbody,
                    GetCurrentHeadingDegrees(), engines, forward, centerOfMass, maxLeverArm, GetSasSettings())
                : turnInput;
            var desiredDirectionPerEngine = new Vector2[engines.Count];

            for (var i = 0; i < engines.Count; i++)
            {
                var engine = engines[i];
                if (engine.MaxThrust <= 0f)
                {
                    engine.RotateThrusterTowards(0f, deltaTime);
                    engine.SetCurrentThrust(0f);
                    desiredDirectionPerEngine[i] = Vector2.zero;
                    continue;
                }

                var desiredDirection = _engineDirectionSolver.GetDesiredEngineDirection(
                    forward, centerOfMass, maxLeverArm, engine, forwardInput, finalTurnInput);
                desiredDirectionPerEngine[i] = desiredDirection;

                if (desiredDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    engine.RotateThrusterTowards(0f, deltaTime);
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                var desiredAngle = Vector2.SignedAngle(engine.Transform.up, desiredDirection.normalized);
                engine.RotateThrusterTowards(desiredAngle, deltaTime);
            }

            var thrustRatios = _controlAllocator.Allocate(engines, desiredDirectionPerEngine, centerOfMass, forward,
                forwardInput, finalTurnInput, maxLeverArm, GetControlAllocatorSettings());

            var anyForceApplied = false;

            for (var i = 0; i < engines.Count; i++)
            {
                var engine = engines[i];

                if (engine.MaxThrust <= 0f)
                {
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                var thrust = Mathf.Clamp01(thrustRatios[i]) * engine.MaxThrust;
                engine.SetCurrentThrust(thrust);

                if (thrust <= Mathf.Epsilon) continue;

                var force = engine.WorldThrustDirection * thrust;

                selfRigidbody.AddForceAtPosition(force, engine.WorldThrustPoint);
                anyForceApplied |= force.sqrMagnitude > Mathf.Epsilon;
            }

            return anyForceApplied;
        }

        private ControlAllocatorSettings GetControlAllocatorSettings()
        {
            return new ControlAllocatorSettings(allocatorIterations, allocatorForceWeight,
                allocatorTorqueWeight, allocatorRegularization);
        }

        private SasTurnInputSettings GetSasSettings()
        {
            return new SasTurnInputSettings(sasTurnReleaseThreshold, sasHeadingDeadZoneDegrees, sasHeadingGain,
                sasAngularVelocityGain, sasMaxTurnInput, sasForwardCompensationStrength,
                sasForwardCompensationMaxTurnInput);
        }

        private float GetCurrentHeadingDegrees()
        {
            return CommandModule.Transform.eulerAngles.z;
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

#if UNITY_INCLUDE_TESTS
        internal void ApplyEngineForcesForTesting(float forwardInput, float turnInput, float deltaTime,
            bool sasEnabled = false)
        {
            ApplyEngineForces(forwardInput, turnInput, deltaTime, sasEnabled);
        }

        internal void ConfigureAllocatorForTesting(bool isEnabled, int iterations = 24, float forceWeight = 1f,
            float torqueWeight = 0.35f, float regularization = 0.001f)
        {
            allocatorIterations = iterations;
            allocatorForceWeight = forceWeight;
            allocatorTorqueWeight = torqueWeight;
            allocatorRegularization = regularization;
        }
#endif
    }
}