using System.Collections.Generic;
using System.Linq;
using LM;
using Ship.Modules;
using UnityEngine;

namespace Ship
{
    public class Ship : MonoBehaviour
    {
        [field: SerializeField] public Command CommandModule { get; private set; }

        [SerializeField] private ModuleConnectionFactory moduleConnectionFactory;

        protected readonly DefaultDictionary<ModuleType, List<Module>> Modules = new(() => new List<Module>());

        public List<IWeapon> Weapons => Modules[ModuleType.Weapon].Cast<IWeapon>().ToList();
        public List<Engine> Engines => Modules[ModuleType.Engine].Cast<Engine>().ToList();

        private void Start()
        {
            CommandModule ??= GetComponentInChildren<Command>();

            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _ => Destroy(gameObject);

            moduleConnectionFactory.ConnectModules(this);
        }

        private void Update()
        {
            Move();

            HandleWeapons();
        }

        public void AddModule(Module module)
        {
            Modules[module.Type].Add(module);
        }

        public void RemoveModule(Module module)
        {
            Modules[module.Type].Remove(module);
        }

        protected virtual void Move()
        {
        }

        protected virtual void HandleWeapons()
        {
        }
    }
}