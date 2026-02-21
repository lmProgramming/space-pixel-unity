using System;
using System.Collections.Generic;
using Core.Gameplay.Combat;
using Core.Gameplay.EasyTeam;
using Core.Services;
using Core.Ship;
using Cysharp.Threading.Tasks;
using Gameplay.EasyTeam;
using LM;
using LM.Graph;
using Ships.Internal;
using Ships.Modules;
using UnityEngine;
using Zenject;
using ZLinq;

namespace Ships
{
    public class Ship : MonoBehaviour, IShip
    {
        private const float UpdateResourcesTimer = 0.1f;
        [SerializeField] private ModuleConnectionFactory moduleConnectionFactory;

        [SerializeField]
        private Team team;

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modulesDictionary = new(() => new List<Module>());

        private BiCohesionGraph<IModule> _biCohesionGraph;

        [Inject]
        private IMapInfo _mapInfo;

        [Inject]
        protected IShipService ShipService;

        private List<Module> AllModules =>
            ModuleGraph.GetAllNodes().AsValueEnumerable().OfType<Module>().ToList();

        public float GeneralEfficiency => Math.Max(0.01f, ResourceManager.EnergyEfficiency);

        public Graph<IModule> ModuleGraph => _biCohesionGraph;

        public Vector2 AttackTargetPosition { get; protected set; }

        public List<IWeapon> Weapons =>
            _modulesDictionary[ModuleType.Weapon].AsValueEnumerable().Cast<IWeapon>().ToList();

        public List<Engine> Engines =>
            _modulesDictionary[ModuleType.Engine].AsValueEnumerable().Cast<Engine>().ToList();

        public ResourceManager ResourceManager { get; private set; }

        protected virtual void Start()
        {
            CommandModule ??= GetComponentInChildren<Command>();
            ResourceManager ??= GetComponentInChildren<ResourceManager>();

            _biCohesionGraph = new BiCohesionGraph<IModule>(CommandModule);
            _biCohesionGraph.OnNodesRemovedDueToUnreachability += HandleUnreachableModules;

            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _ => Destroy(gameObject);

            moduleConnectionFactory.ConnectModules(this);

            RecacheModulesDictionary();

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
        }

        public ITeam Team
        {
            get => team;
            private set => team = value as Team;
        }

        public IModule CommandModule { get; private set; }

        public Vector2 GetPosition()
        {
            return CommandModule.Transform.position;
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

        public void OnModuleDestroyed(IModule module)
        {
            if (module == null) return;

            Debug.Log($"[Ship] Module destroyed: {module.Transform.name}", module.Transform);

            _biCohesionGraph.RemoveNode(module);
            RecacheModulesDictionary();
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

            foreach (var module in ModuleGraph.GetAllNodes()) _modulesDictionary[module.Type].Add(module as Module);

            ResourceManager.Recalculate(AllModules);
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