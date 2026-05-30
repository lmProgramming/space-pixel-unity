using Core.Services;
using UnityEngine;
using Zenject;

namespace Ships
{
    public class PlayerShip : Ship
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;
        [SerializeField] private bool sasEnabled = true;
        [SerializeField] private KeyCode sasToggleKey = KeyCode.T;

        [Inject] private IGameInput _gameInput;

        public bool SasEnabled => sasEnabled;

        public void ToggleSas()
        {
            sasEnabled = !sasEnabled;
        }

        protected override void Move()
        {
            if (!_gameInput.CanControlShip)
                return;

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
            if (!_gameInput.CanFireWeapons)
            {
                StopShooting();
                return;
            }

            AttackTargetPosition = _gameInput.WorldPointerPosition;

            if (Input.GetMouseButton(0) && _gameInput.CanFireWeapons) Shoot();

            if (Input.GetMouseButtonUp(0)) StopShooting();
        }
    }
}