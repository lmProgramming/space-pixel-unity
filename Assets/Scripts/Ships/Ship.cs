using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Core.Constants;
using Core.Gameplay.EasyTeam;
using Core.Pixelation;
using Core.Services;
using Core.Ships;
using Core.Ships.Module;
using Cysharp.Threading.Tasks;
using Events.Gameplay.Ship;
using Gameplay.EasyTeam;
using LMPro;
using LMPro.DataStructures;
using LMPro.DataStructures.Graph;
using LMPro.External.IsAlive;
using Ships.ModuleConnection;
using Ships.Modules;
using Ships.Systems.Gimbal;
using Ships.Systems.Resources;
using UnityEngine;
using Zenject;
using ZLinq;

[assembly: InternalsVisibleTo("Ships.Tests")]
[assembly: InternalsVisibleTo("E2E")]

namespace Ships
{
    [RequireComponent(typeof(ResourceManager))]
    [DisallowMultipleComponent]
    public class Ship : MonoBehaviour, IShip
    {
        private const float UpdateResourcesTimer = 0.1f;

        [SerializeField]
        private Team team;

        [Header("SAS")] [SerializeField] private SASTurnInputSettings sasTurnInputSettings;

        [Header("Control Allocator")]
        [SerializeField] private int allocatorIterations = 14;

        [SerializeField] private float allocatorForceWeight = 1f;
        [SerializeField] private float allocatorTorqueWeight = 0.4f;
        [SerializeField] private float allocatorRegularization = 0.02f;

        private readonly List<Module> _allModulesCache = new();

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modulesDictionary = new(() => new List<Module>());

        private BiCohesionGraph<IModule> _biCohesionGraph;

        [Inject] private DiContainer _container;

        private ControlAllocator _controlAllocator;

        private bool _destroyRequested;

        private EngineDirectionSolver _engineDirectionSolver;

        [Inject]
        private IMapInfo _mapInfo;

        private IModuleConnectionFactory _moduleConnectionFactory;

        [Inject] private IModuleRestoreFactory _moduleRestoreFactory;

        private Action<IPixelated> _onCommandModuleNoPixelsLeft;

        private SASTurnInputResolver _sasTurnInputResolver;

        [InjectOptional] private SceneContextRegistry _sceneContextRegistry;

        [Inject]
        private ShipInitializeModulesEventChannel _shipInitializeModulesEventChannel;


        protected float PendingForwardInput;
        protected float PendingHorizontalInput;
        protected float PendingTurnInput;

        [Inject]
        protected IShipService ShipService;

        internal IModuleConnectionFactory ModuleConnectionFactoryForTesting
        {
            set => _moduleConnectionFactory = value;
        }

        public int CrewMissingCount =>
            ModuleGraph.GetAllNodes().AsValueEnumerable().Sum(module => module.CrewMissingCount);

        private void Awake()
        {
            ResourceManager = GetComponent<ResourceManager>();
            _moduleConnectionFactory = GetComponent<IModuleConnectionFactory>();
            _engineDirectionSolver = new EngineDirectionSolver();
            _controlAllocator = new ControlAllocator();
            _sasTurnInputResolver = new SASTurnInputResolver();

            if (ResourceManager == null)
                throw new UnityException("[Ship] ResourceManager is required.");
            if (_moduleConnectionFactory == null)
                throw new UnityException("[Ship] IModuleConnectionFactory is required.");
            if (_engineDirectionSolver == null)
                throw new InvalidOperationException("[Ship] EngineDirectionSolver failed to initialize.");
            if (_controlAllocator == null)
                throw new InvalidOperationException("[Ship] ControlAllocator failed to initialize.");
            if (_sasTurnInputResolver == null)
                throw new InvalidOperationException("[Ship] SASTurnInputResolver failed to initialize.");
        }

        protected virtual void Start()
        {
            CommandModule ??= GetComponentInChildren<Command>();

            if (CommandModule == null)
                throw new UnityException("[Ship] CommandModule is required.");

            InitializeModules();
            _sasTurnInputResolver.CaptureDesiredHeading(GetCurrentHeadingDegrees());

            if (!IsDesignMode)
                UpdateResourcesLoop().Forget();
        }

        protected virtual void Update()
        {
            if (IsDesignMode) return;

            ReadMovementInput();
            HandleWeapons();
            ResourceManager.UpdateEnergy();
        }

        private void FixedUpdate()
        {
            if (IsDesignMode) return;

            ApplyMovementPhysics();
        }

        private void OnEnable()
        {
            if (IsDesignMode) return;

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

        public IResourceManager ResourceManager { get; private set; }

        public List<IWeapon> Weapons =>
            _modulesDictionary[ModuleType.Weapon].AsValueEnumerable().Cast<IWeapon>().Where(e => e.IsAliveEnabled())
                .ToList();

        public List<IEngine> Engines =>
            _modulesDictionary[ModuleType.Engine].AsValueEnumerable().Cast<IEngine>().Where(e => e.IsAliveEnabled())
                .ToList();

        public string Name => transform.name;
        public IReadOnlyList<IModule> AllModules => _allModulesCache;
        public virtual bool IsSASOn => false;
        public bool IsDesignMode { get; set; }

        public float GeneralEfficiency => Math.Max(0.01f, ResourceManager.EnergyEfficiency);

        public Graph<IModule> ModuleGraph => _biCohesionGraph;

        public Vector2 AttackTargetPosition { get; protected set; }

        public float CaptainMultiplier => CommandModule.GetCrewEfficiency();

        public ITeam Team => team;

        public IModule CommandModule { get; private set; }

        public Collider2D[] OwnColliders { get; private set; } = Array.Empty<Collider2D>();

        public Vector2 GetPosition()
        {
            return CommandModule.PixelatedRigidbody.WorldWeightedCenter;
        }

        public void OnModuleConnectionLost(IModule module)
        {
            if (module == null) return;

            if (module == CommandModule) DestroyShip();

            Debug.Log($"[Ship] Module destroyed: {module.Transform?.name}", module.Transform);

            _biCohesionGraph.RemoveNode(module);
            HandleModuleChange();

            DeIgnoreCollider(module.Collider2D);
        }

        public void ManualAddModule(IModule module)
        {
            if (module == null || !module.Transform) throw new ArgumentNullException(nameof(module));
            module.Transform.SetParent(transform);
            InitializeModules();
        }

        public void ManualRemoveModule(IModule module)
        {
            switch (module)
            {
                case null:
                    throw new ArgumentNullException(nameof(module));
                case Module concreteModule:
                    concreteModule.DetachAllConnections();
                    break;
            }

            if (!module.Transform)
            {
                Debug.LogError($"[Ship] ManualRemoveModule: module.Transform is null for module {module}", this);
                return;
            }

            module.Transform.SetParent(null);
            InitializeModules();
        }

        public void DestroyAllModules()
        {
            var existingModules = GetComponentsInChildren<Module>();

            foreach (var module in existingModules)
            {
                module.OnShipConnectionLost();
                module.transform.SetParent(null, true);
                Destroy(module.gameObject);
            }
        }

        public void InitializeModules()
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

            _onCommandModuleNoPixelsLeft = _ => DestroyShip();
            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _onCommandModuleNoPixelsLeft;

            _moduleConnectionFactory.ConnectModules(this);

            _shipInitializeModulesEventChannel.Raise();

            HandleModuleChange();

            IgnoreModuleColliders();
        }


        public void SetTeam(ITeam newTeam)
        {
            team = newTeam as Team;
            if (team == null)
                throw new ArgumentException("[Ship] newTeam must be of type Team.");
            gameObject.layer = team.Layer;
            transform.SetLayerAllChildren(team.Layer);
        }

        public void DestroyAllModulesSilently()
        {
            var existingModules = GetComponentsInChildren<Module>();

            foreach (var module in existingModules)
            {
                module.SetShip(null);
                module.transform.SetParent(null, true);
                Destroy(module.gameObject);
            }
        }

        public ShipSnapshot CaptureSnapshot(IGameContentCatalog contentCatalog)
        {
            if (!this.IsAlive())
            {
                Debug.LogError("[Ship] Cannot capture snapshot: ship is null");
                return null;
            }

            if (contentCatalog == null) throw new ArgumentNullException(nameof(contentCatalog));

            var snapshot = new ShipSnapshot(Name);

            foreach (var module in AllModules)
            {
                var moduleSnapshot = module.CaptureSnapshot(contentCatalog);
                snapshot.modules.Add(moduleSnapshot);

                if (CommandModule != module) continue;

                if (snapshot.commandModuleInstanceId != null)
                    throw new InvalidOperationException(
                        "[Ship] Multiple command modules found. Only the first one will be recorded in the snapshot.");

                snapshot.commandModuleInstanceId = moduleSnapshot.instanceId;
            }

            Debug.Log($"[Ship] Captured snapshot of '{Name}' with {snapshot.modules.Count} modules");
            return snapshot;
        }

        public void RestoreFromSnapshot(ShipSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (contentCatalog == null) throw new ArgumentNullException(nameof(contentCatalog));

            DestroyAllModulesSilently();

            CreateModulesFromSnapshot(snapshot, contentCatalog);

            Debug.Log(
                $"[Ship] Applied snapshot '{snapshot.shipName}' to '{Name}' ({snapshot.modules.Count} modules)");
        }

        private void CreateModulesFromSnapshot(ShipSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            foreach (var ms in snapshot.modules)
            {
                var moduleGo = _moduleRestoreFactory.CreateModuleShell(ms, transform);
                moduleGo.SetActive(false);
                moduleGo.transform.localPosition = ms.localPosition;
                moduleGo.transform.localRotation = ms.localRotation;

                var identity = moduleGo.GetComponent<GameObjectInstanceIdentity>();
                if (!identity)
                    identity = moduleGo.AddComponent<GameObjectInstanceIdentity>();
                identity.RestoreFromSnapshot(ms.instanceId, ms.origin, ms.archetypeId);

                var module = moduleGo.GetComponent<IModule>();
                if (module == null)
                    throw new UnityException(
                        $"[Ship] Failed to add a Module component for '{ms.moduleName}' (moduleType: {ms.concreteModuleType}).");

                module.SetShip(this);
                module.RestoreFromSnapshot(ms, contentCatalog);

                moduleGo.SetActive(true);
                moduleGo.gameObject.layer = gameObject.layer;
            }
        }

        private void IgnoreModuleColliders()
        {
            var combinations = from item1 in OwnColliders.AsValueEnumerable()
                from item2 in OwnColliders.AsValueEnumerable()
                where item1.GetHashCode() < item2.GetHashCode()
                select Tuple.Create(item1, item2);
            foreach (var combination in combinations) Physics2D.IgnoreCollision(combination.Item1, combination.Item2);
        }

        private void DeIgnoreCollider(Collider2D other)
        {
            if (!other) return;

            foreach (var item1 in OwnColliders) Physics2D.IgnoreCollision(other, item1, false);
        }

        private void DestroyShip()
        {
            if (_destroyRequested) return;
            _destroyRequested = true;

            Debug.Log($"[Ship] Command module destroyed. Destroying ship: {gameObject.name}", gameObject);

            // Unity's Destroy disables every component in the hierarchy immediately, so any module
            // still parented at that point would be reparented out later (HandleUnreachableModules)
            // with its components permanently disabled. Release survivors while they are still enabled.
            ReleaseSurvivingModulesAsJunk();

            Destroy(gameObject);
        }

        private void ReleaseSurvivingModulesAsJunk()
        {
            var survivors = ModuleGraph.GetAllNodes().AsValueEnumerable()
                .Where(module => module != CommandModule)
                .ToList();

            foreach (var module in survivors) ReleaseModuleAsJunk(module);
        }

        private void ReleaseModuleAsJunk(IModule module)
        {
            // If the ship hierarchy is already being torn down, its components are disabled;
            // rescuing modules out of it would leave zombie junk with disabled components.
            if (!isActiveAndEnabled) return;

            if (!module.IsAliveEnabled() || !module.Transform ||
                module.Transform.parent == _mapInfo.MapTransform) return;

            Debug.Log($"[Ship] Releasing module as junk: {module.Transform.name}", module.Transform);
            module.Transform.SetParent(_mapInfo.MapTransform);
            module.Transform.gameObject.layer = PhysicsLayers.Default;

            if (module is Module concreteModule) concreteModule.OnShipConnectionLost();
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

            foreach (var module in unreachableModules) ReleaseModuleAsJunk(module);

            HandleModuleChange();
        }

        private void HandleModuleChange()
        {
            _modulesDictionary.Clear();
            _allModulesCache.Clear();

            foreach (var module in ModuleGraph.GetAllNodes())
            {
                var mod = module as Module;
                _modulesDictionary[module.Type].Add(mod);
                _allModulesCache.Add(mod);
            }

            OwnColliders = ModuleGraph.GetAllNodes().AsValueEnumerable()
                .SelectMany(m => m.Transform?.GetComponentsInChildren<Collider2D>())
                .ToArray();

            ResourceManager.Recalculate(_allModulesCache);

            if (IsDesignMode)
                ConfigureModulesForDesignMode();
        }

        private void ConfigureModulesForDesignMode()
        {
            foreach (var module in _allModulesCache)
            {
                var rigidbody = module.PixelatedRigidbody?.Rigidbody;
                if (rigidbody != null)
                    rigidbody.simulated = false;

                if (module is Engine engine)
                    engine.SuppressNozzleExhaustForDesignMode();
            }

            MarkEnginesActivity(false);
        }

        protected virtual void ReadMovementInput()
        {
        }

        protected virtual void ApplyMovementPhysics()
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
            var crewList = crew.AsValueEnumerable().ToList();

            foreach (var module in ModuleGraph.GetAllNodes())
            {
                module.FillCrewBySkill(crewList, out var remainingCrew);
                crewList = remainingCrew.AsValueEnumerable().ToList();
            }
        }

        protected void MarkEnginesActivity(bool active)
        {
            foreach (var engine in Engines)
                engine.SetActive(active);
        }

        protected bool ApplyEngineForces(float forwardInput, float horizontalInput, float turnInput, float deltaTime,
            bool sasEnabled = false)
        {
            if (sasEnabled)
            {
                forwardInput = ApplyInputDeadZone(forwardInput, sasTurnInputSettings.MovementInputDeadZone);
                horizontalInput = ApplyInputDeadZone(horizontalInput, sasTurnInputSettings.MovementInputDeadZone);
                turnInput = ApplyInputDeadZone(turnInput, sasTurnInputSettings.TurnReleaseThreshold);
            }

            forwardInput = Mathf.Clamp(forwardInput, -1f, 1f);
            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            turnInput = Mathf.Clamp(turnInput, -1f, 1f);

            var selfRigidbody = CommandModule.PixelatedRigidbody?.Rigidbody;
            if (selfRigidbody == null)
                throw new InvalidOperationException("[Ship] CommandModule.PixelatedRigidbody.Rigidbody is required.");
            if (CommandModule.Transform == null)
                throw new InvalidOperationException("[Ship] CommandModule.Transform is required.");

            var engines = Engines;
            if (engines.Count == 0) return false;

            var forward = CommandModule.Transform.up;
            var centerOfMass = selfRigidbody.worldCenterOfMass;
            var maxLeverArm = EngineDirectionSolver.GetMaxLeverArmLength(engines, centerOfMass);
            var finalTurnInput = sasEnabled
                ? _sasTurnInputResolver.ResolveTurnInput(turnInput, forwardInput, horizontalInput, selfRigidbody,
                    GetCurrentHeadingDegrees(), engines, forward, centerOfMass, maxLeverArm, sasTurnInputSettings)
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

                var desiredDirection = EngineDirectionSolver.GetDesiredEngineDirection(
                    forward, centerOfMass, maxLeverArm, engine, forwardInput, horizontalInput, finalTurnInput);
                desiredDirectionPerEngine[i] = desiredDirection;

                if (desiredDirection.sqrMagnitude <= Mathf.Epsilon)
                {
                    engine.RotateThrusterTowards(0f, deltaTime);
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                if (!engine.Transform)
                {
                    Debug.LogWarning("[Ship] Engine has null Transform. Skipping thruster rotation.",
                        this);
                    desiredDirectionPerEngine[i] = Vector2.zero;
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                var desiredAngle = Vector2.SignedAngle(engine.Transform.up, desiredDirection.normalized);
                engine.RotateThrusterTowards(desiredAngle, deltaTime);
            }

            var thrustRatios = ControlAllocator.AllocateControlInputs(engines, desiredDirectionPerEngine, centerOfMass,
                forward,
                forwardInput, horizontalInput, finalTurnInput, maxLeverArm, GetControlAllocatorSettings());

            var anyForceApplied = false;

            for (var i = 0; i < engines.Count; i++)
            {
                var engine = engines[i];

                if (!engine.IsAliveEnabled() || engine.MaxThrust <= 0f)
                {
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                var thrust = Mathf.Clamp01(thrustRatios[i]) * engine.MaxThrust;
                if (thrust <= engine.MaxThrust * sasTurnInputSettings.MinAppliedThrustRatio)
                {
                    engine.SetCurrentThrust(0f);
                    continue;
                }

                engine.SetCurrentThrust(thrust);

                var force = engine.WorldThrustDirection * thrust;

                selfRigidbody.AddForceAtPosition(force, engine.WorldThrustPoint);
                anyForceApplied |= force.sqrMagnitude > Mathf.Epsilon;
            }

            return anyForceApplied;
        }

        private static float ApplyInputDeadZone(float input, float deadZone)
        {
            return Mathf.Abs(input) <= deadZone ? 0f : input;
        }

        private ControlAllocatorSettings GetControlAllocatorSettings()
        {
            return new ControlAllocatorSettings(allocatorIterations, allocatorForceWeight,
                allocatorTorqueWeight, allocatorRegularization);
        }

        private float GetCurrentHeadingDegrees()
        {
            if (!CommandModule.Transform)
            {
                Debug.LogWarning("[Ship] GetCurrentHeadingDegrees: CommandModule.Transform is null");
                return 0f;
            }

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
        internal void SetSASDesiredHeadingForTesting(float headingDegrees)
        {
            _sasTurnInputResolver.CaptureDesiredHeading(headingDegrees);
        }

        internal float GetHeadingDegreesForTesting()
        {
            return GetCurrentHeadingDegrees();
        }

        internal Vector2 GetForwardForTesting()
        {
            return CommandModule.Transform!.up;
        }

        internal void ApplyEngineForcesForTesting(float forwardInput, float horizontalInput, float turnInput,
            float deltaTime,
            bool sasEnabled = false)
        {
            ApplyEngineForces(forwardInput, horizontalInput, turnInput, deltaTime, sasEnabled);
        }

        internal void ConfigureAllocatorForTesting(bool isEnabled, int iterations = 24, float forceWeight = 1f,
            float torqueWeight = 0.35f, float regularization = 0.001f)
        {
            allocatorIterations = iterations;
            allocatorForceWeight = forceWeight;
            allocatorTorqueWeight = torqueWeight;
            allocatorRegularization = regularization;
        }

        internal void ConfigureSASSettingsForTesting(SASTurnInputSettings sasSettings)
        {
            sasTurnInputSettings = sasSettings;
        }
#endif
    }
}