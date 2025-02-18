using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ship
{
    public class Ship : MonoBehaviour
    {
        [field: SerializeField] public ShipBody Body { get; private set; }

        [SerializeField] protected List<IWeapon> Weapons = new();

        private void Start()
        {
            Weapons = GetComponentsInChildren<IWeapon>().ToList();
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