using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ship
{
    public class Ship : MonoBehaviour
    {
        [field: SerializeField] public ShipBody Body { get; private set; }

        protected List<IWeapon> Weapons = new();

        private void Start()
        {
            Weapons = GetComponentsInChildren<IWeapon>().ToList();

            Body.OnNoPixelsLeft += _ => Destroy(gameObject);
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