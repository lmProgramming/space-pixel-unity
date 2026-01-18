using System;
using System.Threading;
using Core.Gameplay.Combat;
using Core.Services;
using Core.Ship;
using Cysharp.Threading.Tasks;
using LM;
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

        private SimpleTimer _reloadTimer;

        protected override void Awake()
        {
            base.Awake();
            Type = ModuleType.Weapon;
        }

        private void Start()
        {
            _reloadTimer = new SimpleTimer(reloadTime);
            _cts = new CancellationTokenSource();

            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;
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
            if (!Ship) return;

            var targetPosition = Ship.AttackTargetPosition;

            if (!transform) return;
            var direction = (targetPosition - (Vector2)transform.position).normalized;

            var angle = MathExt.AngleBetweenTwoPoints(targetPosition, transform.position);

            var rotation = Quaternion.Euler(0, 0, angle - 90);

            var newBullet =
                _projectilesSpawner.Spawn(projectilePrefab, transform.position, rotation, gameObject.layer);

            var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
            bulletRigidbody.linearVelocity = PixelatedRigidbody.Rigidbody.linearVelocity;
            bulletRigidbody.AddForce(PixelatedRigidbody.Rigidbody.linearVelocity + direction * projectileSpeed,
                ForceMode2D.Impulse);

            _reloadTimer.Wait(reloadTime, _cts.Token).Forget();
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