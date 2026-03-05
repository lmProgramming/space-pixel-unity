using LMPro;
using UnityEngine;
using ZLinq;

namespace Ships
{
    public class PlayerShip : Ship
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;

        protected override void Move()
        {
            var availableThrust = Engines.AsValueEnumerable().Sum(e => e.MaxThrust);

            var acceleration = Input.GetAxis("Vertical") * speedMultiplier * availableThrust;

            CommandModule.PixelatedRigidbody.Rigidbody.AddForce(CommandModule.Transform.up * acceleration);

            var turn = Input.GetAxis("Horizontal") * rotationMultiplier * availableThrust;

            CommandModule.PixelatedRigidbody.Rigidbody.AddTorque(turn);

            MarkEnginesActivity(acceleration != 0 || turn != 0);
        }

        protected override void HandleWeapons()
        {
            AttackTargetPosition = GameInput.WorldPointerPosition;

            if (Input.GetMouseButton(0)) Shoot();

            if (Input.GetMouseButtonUp(0)) StopShooting();
        }
    }
}