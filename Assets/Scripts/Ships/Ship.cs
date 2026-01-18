using System.Collections.Generic;
using System.Linq;
using Core;
using LM;
using LM.Graph;
using Ships.Modules;
using UnityEngine;
using Zenject;

namespace Ships
{
    public class Ship : MonoBehaviour, IShip
    {
        [SerializeField] private ModuleConnectionFactory moduleConnectionFactory;

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modules = new(() => new List<Module>());

        [Inject]
        private IShipService _shipService;

        public Graph<IModule> ModuleGraph { get; private set; }

        public Vector2 AttackTargetPosition { get; protected set; }

        public List<IWeapon> Weapons => _modules[ModuleType.Weapon].Cast<IWeapon>().ToList();
        public List<Engine> Engines => _modules[ModuleType.Engine].Cast<Engine>().ToList();

        protected virtual void Start()
        {
            CommandModule ??= GetComponentInChildren<Command>();

            ModuleGraph = new BiCohesionGraph<IModule>(CommandModule);

            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _ => Destroy(gameObject);

            moduleConnectionFactory.ConnectModules(this);

            RecacheModulesDictionary();
        }

        private void Update()
        {
            Move();

            HandleWeapons();
        }

        private void OnEnable()
        {
            _shipService.RegisterShip(this);
        }

        private void OnDisable()
        {
            _shipService.UnregisterShip(this);
        }

        public IModule CommandModule { get; private set; }

        [field: SerializeField]
        public Team Team { get; private set; }

        public Vector2 GetPosition()
        {
            return transform.position;
        }

        public void RecacheModulesDictionary()
        {
            _modules.Clear();

            foreach (var module in ModuleGraph.GetAllNodes()) _modules[module.Type].Add(module as Module);
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

        protected IShip FindClosestEnemy(float maxRange = float.MaxValue)
        {
            var allShips = _shipService.GetEnemyShipsOf(Team);
            IShip closestEnemy = null;
            var closestDistance = maxRange;

            foreach (var ship in allShips)
            {
                var distance = Vector2.Distance(transform.position, ship.GetPosition());
                if (!(distance < closestDistance)) continue;

                closestDistance = distance;
                closestEnemy = ship;
            }

            return closestEnemy;
        }
    }
}