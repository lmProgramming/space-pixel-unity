using System;
using System.Threading;
using Core.Gameplay.Combat;
using Core.Services;
using Core.Ship;
using LMPro;
using UnityEngine;
using Zenject;

namespace Ships.Modules
{
    public class Cannon : Module, IWeapon
    {
        [SerializeField] private GameObject projectilePrefab;

        [SerializeField] private float projectileSpeed;

        [SerializeField] private float reloadTime;

        [SerializeField] private GameObject icon;
        private CancellationTokenSource _cts;

        [Inject] private IProjectilesSpawner _projectilesSpawner;

        private ManualTimer _reloadTimer;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Weapon;

            _reloadTimer = new ManualTimer(reloadTime);
            _cts = new CancellationTokenSource();
        }

        protected override void Start()
        {
            base.Start();
            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;
        }

        public void Update()
        {
            _reloadTimer.Progress(Time.deltaTime * ShipModuleEfficiency);
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_reloadTimer == null) return;
            _reloadTimer.OnReady -= HandleReady;
            _reloadTimer.OnNotReady -= HandleNotReady;
        }

        public void Shoot()
        {
            if (!_reloadTimer.IsReady) return;
            if (Ship == null) return;

            var targetPosition = Ship.AttackTargetPosition;

            if (!transform) return;
            var direction = (targetPosition - (Vector2)transform.position).normalized;

            var angle = MathExt.AngleBetweenTwoPoints(targetPosition, transform.position);

            var rotation = Quaternion.Euler(0, 0, angle - 90);

            var shooterColliders = Ship.OwnColliders;
            var newBullet =
                _projectilesSpawner.Spawn(projectilePrefab, transform.position, rotation, shooterColliders);

            var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
            bulletRigidbody.linearVelocity = PixelatedRigidbody.Rigidbody.linearVelocity;
            bulletRigidbody.AddForce(PixelatedRigidbody.Rigidbody.linearVelocity + direction * projectileSpeed,
                ForceMode2D.Impulse);

            _reloadTimer.Reset();
        }

        public void StopShooting()
        {
        }

        public bool IsReady()
        {
            return _reloadTimer.IsReady;
        }

        public GameObject GetIcon()
        {
            return icon;
        }

        public event Action OnReady;
        public event Action OnNotReady;

        public override float GetEnergyDraw()
        {
            return base.GetEnergyDraw() * (IsReady()
                ? 0.25f
                : 1f);
        }

        private void HandleReady()
        {
            OnReady?.Invoke();
        }

        private void HandleNotReady()
        {
            OnNotReady?.Invoke();
        }
    }
}