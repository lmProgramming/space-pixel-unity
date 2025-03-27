using System;
using System.Collections.Generic;
using System.Linq;
using Ship.Modules;
using UnityEngine;

namespace Ship
{
    public class Ship : MonoBehaviour
    {
        [field: SerializeField] public Command CommandModule { get; private set; }

        [SerializeField] private ModuleConnectionFactory moduleConnectionFactory;

        protected readonly Dictionary<Type, List<Module>> Modules = new();

        public List<IWeapon> Weapons => Modules[typeof(IWeapon)].Cast<IWeapon>().ToList();
        public List<Engine> Engines => Modules[typeof(Engine)].Cast<Engine>().ToList();

        private void Start()
        {
            Modules[typeof(IWeapon)] = GetComponentsInChildren<IWeapon>().Cast<Module>().ToList();
            Modules[typeof(Engine)] = GetComponentsInChildren<Engine>().Cast<Module>().ToList();

            CommandModule ??= GetComponentInChildren<Command>();

            CommandModule.PixelatedRigidbody.OnNoPixelsLeft += _ => Destroy(gameObject);

            moduleConnectionFactory.ConnectModules(this);
        }

        private void Update()
        {
            Move();

            HandleWeapons();
        }

        protected virtual void Move()
        {
        }

        protected virtual void HandleWeapons()
        {
        }
    }
}