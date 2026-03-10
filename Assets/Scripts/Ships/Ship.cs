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
    public class Ship : MonoBehaviour, IShip
    {
        private const float UpdateResourcesTimer = 0.1f;

        [SerializeField]
        private Team team;

        private readonly List<Module> _allModulesCache = new();

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modulesDictionary = new(() => new List<Module>());

        private BiCohesionGraph<IModule> _biCohesionGraph;

        [Inject]
        private IMapInfo _mapInfo;

        private IModuleConnectionFactory _moduleConnectionFactory;

        private Action<IPixelated> _onCommandModuleNoPixelsLeft;

        [Inject]
        private ShipInitializeModulesEventChannelSO _shipInitializeModulesEventChannelSO;

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

        public event Action OnModulesInitialized;

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

        public void StopShooting()
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