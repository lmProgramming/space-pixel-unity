using UnityEngine;

namespace Ship
{
    public class PlayerShip : Ship
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;

        protected override void Move()
        {
            var acceleration = Input.GetAxis("Vertical") * speedMultiplier;

            MainModule.PixelatedRigidbody.Rigidbody.AddForce(MainModule.transform.up * acceleration);

            var turn = Input.GetAxis("Horizontal") * rotationMultiplier;

            MainModule.PixelatedRigidbody.Rigidbody.AddTorque(turn);
        }

        protected override void HandleWeapons()
        {
            if (!Input.GetMouseButton(0)) return;
            foreach (var weapon in Weapons) weapon.Shoot();
        }
    }
}