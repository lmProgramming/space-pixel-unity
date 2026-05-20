using LMPro;
using UnityEngine;

namespace Ships
{
    public class PlayerShip : Ship
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;
        [SerializeField] private bool sasEnabled = true;
        [SerializeField] private KeyCode sasToggleKey = KeyCode.T;

        public bool SasEnabled => sasEnabled;

        public void ToggleSas()
        {
            sasEnabled = !sasEnabled;
        }

        protected override void Move()
        {
            if (Input.GetKeyDown(sasToggleKey))
                ToggleSas();

            var forwardInput = Input.GetAxis("Vertical") * speedMultiplier;
            var turnInput = Input.GetAxis("Horizontal") * rotationMultiplier;

            MarkEnginesActivity(ApplyEngineForces(forwardInput, turnInput, Time.deltaTime, sasEnabled));
        }

        protected override void HandleWeapons()
        {
            AttackTargetPosition = GameInput.WorldPointerPosition;

            if (Input.GetMouseButton(0)) Shoot();

            if (Input.GetMouseButtonUp(0)) StopShooting();
        }
    }
}