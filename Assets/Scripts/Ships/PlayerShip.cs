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

            var rawVertical = Input.GetAxis("Vertical");
            var rawHorizontal = Input.GetAxis("Horizontal");
            var forwardInput = rawVertical * speedMultiplier;
            var turnInput = rawHorizontal * rotationMultiplier;

            MarkEnginesActivity(ApplyEngineForces(forwardInput, turnInput, Time.deltaTime, sasEnabled));
        }

        protected override void HandleWeapons()
        {
            AttackTargetPosition = GameInput.WorldPointerPosition;

            if (Input.GetMouseButton(0) && !GameInput.IsPointerOverUI) Shoot();

            if (Input.GetMouseButtonUp(0)) StopShooting();
        }
    }
}