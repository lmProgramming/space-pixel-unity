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

        protected List<IWeapon> Weapons = new();

        private void Start()
        {
            Weapons = GetComponentsInChildren<IWeapon>().ToList();

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