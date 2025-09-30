using UnityEngine;

namespace Ships
{
    public class PlayerShip : Ship
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;

        protected override void Move()
        {
            var engineCount = Engines.Count + 1;

            var acceleration = Input.GetAxis("Vertical") * speedMultiplier * engineCount;

            CommandModule.PixelatedRigidbody.Rigidbody.AddForce(CommandModule.transform.up * acceleration);

            var turn = Input.GetAxis("Horizontal") * rotationMultiplier * engineCount;

            CommandModule.PixelatedRigidbody.Rigidbody.AddTorque(turn);
        }

        protected override void HandleWeapons()
        {
            if (Input.GetMouseButton(0))
                foreach (var weapon in Weapons)
                    weapon.Shoot();

            if (Input.GetMouseButtonUp(0))
                foreach (var weapon in Weapons)
                    weapon.StopShooting();
        }
    }
}