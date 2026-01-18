using LM;
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

            CommandModule.PixelatedRigidbody.Rigidbody.AddForce(CommandModule.Transform.up * acceleration);

            var turn = Input.GetAxis("Horizontal") * rotationMultiplier * engineCount;

            CommandModule.PixelatedRigidbody.Rigidbody.AddTorque(turn);
        }

        protected override void HandleWeapons()
        {
            AttackTargetPosition = GameInput.WorldPointerPosition;

            if (Input.GetMouseButton(0)) Shoot();

            if (Input.GetMouseButtonUp(0)) StopShooting();
        }
    }
}