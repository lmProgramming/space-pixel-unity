using System.Collections.Generic;
using System.Linq;
using LM;
using LM.Graph;
using Ships.Modules;
using UnityEngine;

namespace Ships
{
    public class Ship : MonoBehaviour
    {
        [SerializeField] private ModuleConnectionFactory moduleConnectionFactory;

        // ReSharper disable once CollectionNeverUpdated.Local
        private readonly DefaultDictionary<ModuleType, List<Module>> _modules = new(() => new List<Module>());
        public Command CommandModule { get; private set; }

        public Graph<Module> ModuleGraph { get; private set; }

        public List<IWeapon> Weapons => _modules[ModuleType.Weapon].Cast<IWeapon>().ToList();
        public List<Engine> Engines => _modules[ModuleType.Engine].Cast<Engine>().ToList();

        protected virtual void Start()
        {
            CommandModule ??= GetComponentInChildren<Command>();

            ModuleGraph = new BiCohesionGraph<Module>(CommandModule);

            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _ => Destroy(gameObject);

            moduleConnectionFactory.ConnectModules(this);

            RecacheModulesDictionary();
        }

        private void Update()
        {
            Move();

            HandleWeapons();
        }

        public void RecacheModulesDictionary()
        {
            _modules.Clear();

            foreach (var module in ModuleGraph.GetAllNodes()) _modules[module.Type].Add(module);
        }

        protected virtual void Move()
        {
        }

        protected virtual void HandleWeapons()
        {
        }
    }
}