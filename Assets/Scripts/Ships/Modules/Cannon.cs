using System;
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

        [Inject] private ProjectilesSpawner _projectilesSpawner;

        private SimpleTimer _reloadTimer;

        private void Start()
        {
            _reloadTimer = new SimpleTimer(reloadTime);

            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;
        }

        private void OnDestroy()
        {
            if (_reloadTimer == null) return;
            _reloadTimer.OnReady -= HandleReady;
            _reloadTimer.OnNotReady -= HandleNotReady;
        }

        public void Shoot()
        {
            if (!_reloadTimer.IsReady) return;

            var pointerPosition = GameInput.WorldPointerPosition;

            var direction = (pointerPosition - (Vector2)transform.position).normalized;

            var angle = MathExt.AngleBetweenTwoPoints(pointerPosition, transform.position);

            var rotation = Quaternion.Euler(0, 0, angle - 90);

            var newBullet =
                _projectilesSpawner.Spawn(projectilePrefab, transform.position, rotation, gameObject.layer);

            var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
            bulletRigidbody.linearVelocity = PixelatedRigidbody.Rigidbody.linearVelocity;
            bulletRigidbody.AddForce(PixelatedRigidbody.Rigidbody.linearVelocity + direction * projectileSpeed,
                ForceMode2D.Impulse);

            _reloadTimer.Wait(reloadTime).Forget();
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