using Core.Services;
using Core.Ships;
using UnityEngine;
using Zenject;

namespace Ships
{
    public class PlayerShip : Ship, ISAS
    {
        [SerializeField] private float speedMultiplier;

        [SerializeField] private float rotationMultiplier;
        [SerializeField] private bool sasEnabled = true;
        [SerializeField] private KeyCode sasToggleKey = KeyCode.T;

        [Inject] private IGameInput _gameInput;

        protected override void Start()
        {
            base.Start();
            foreach (var module in AllModules) module.Transform!.tag = "Player";
        }

        public override bool IsSASOn => sasEnabled;

        public void ToggleSAS()
        {
            sasEnabled = !sasEnabled;
        }

        protected override void ReadMovementInput()
        {
            if (!_gameInput.CanControlShip)
                return;

            if (Input.GetKeyDown(sasToggleKey))
                ToggleSAS();

            PendingForwardInput = Input.GetAxis("Vertical") * speedMultiplier;
            PendingHorizontalInput = Input.GetAxis("Horizontal") * speedMultiplier;
            PendingTurnInput = -Input.GetAxis("Roll") * rotationMultiplier;
        }

        protected override void ApplyMovementPhysics()
        {
            if (!_gameInput.CanControlShip)
            {
                MarkEnginesActivity(false);
                return;
            }

            MarkEnginesActivity(ApplyEngineForces(PendingForwardInput, PendingHorizontalInput, PendingTurnInput,
                Time.fixedDeltaTime,
                sasEnabled));
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