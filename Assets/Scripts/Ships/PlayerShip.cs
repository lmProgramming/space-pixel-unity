using System;
using Core.Services;
using Core.Ships;
using Events.Camera;
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

        private CameraMode _cameraMode;
        [Inject] private CameraModeEventChannel _cameraModeEventChannel;

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

            if (Input.GetKeyDown(KeyCode.F))
                ToggleCameraMode();

            switch (_cameraMode)
            {
                case CameraMode.FreeMode:
                    PendingForwardInput = 0;
                    PendingHorizontalInput = 0;
                    PendingTurnInput = 0;
                    return;
                case CameraMode.FollowingObject:
                    PendingForwardInput = Input.GetAxis("Vertical") * speedMultiplier;
                    PendingHorizontalInput = Input.GetAxis("Horizontal") * speedMultiplier;
                    PendingTurnInput = -Input.GetAxis("Roll") * rotationMultiplier;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ToggleCameraMode()
        {
            _cameraMode = _cameraMode switch
            {
                CameraMode.FollowingObject => CameraMode.FreeMode,
                CameraMode.FreeMode => CameraMode.FollowingObject,
                _ => throw new ArgumentOutOfRangeException()
            };

            _cameraModeEventChannel.Raise(_cameraMode);
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