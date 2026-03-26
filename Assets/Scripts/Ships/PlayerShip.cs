using LMPro;
using UnityEngine;

namespace Ships
{
    public class PlayerShip : Ship
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;

        protected override void Move()
        {
            var forwardInput = Input.GetAxis("Vertical") * speedMultiplier;
            var turnInput = Input.GetAxis("Horizontal") * rotationMultiplier;

            MarkEnginesActivity(ApplyEngineForces(forwardInput, turnInput, Time.deltaTime));
        }

        protected override void HandleWeapons()
        {
            AttackTargetPosition = GameInput.WorldPointerPosition;

            if (Input.GetMouseButton(0)) Shoot();

            if (Input.GetMouseButtonUp(0)) StopShooting();
        }
    }
}