using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Core.Constants;
using Core.Services;
using Core.Ships;
using Core.Ships.Snapshots.Module.ModuleData;
using Events.Gameplay.Shooting;
using LMPro;
using UnityEngine;
using Zenject;
using ZLinq;

[assembly: InternalsVisibleTo("E2E")]
[assembly: InternalsVisibleTo("Ships.Tests")]

namespace Ships.Modules
{
    public class Cannon : WeaponBase
    {
        [SerializeField] private GameObject projectilePrefab;

        [SerializeField] private float projectileSpeed;

        [SerializeField] private List<Transform> projectileSpawnPoints = new();

        [SerializeField] private float reloadTime;

        [SerializeField] private Sprite sprite;
        private CancellationTokenSource _cts;

        [Inject] private IProjectilesSpawner _projectilesSpawner;

        private ManualTimer _reloadTimer;
        [Inject] private ShootingEventChannel _shootingEventChannel;

        public override ConcreteModuleType ConcreteType => ConcreteModuleType.Cannon;

        protected override void Awake()
        {
            base.Awake();

            _reloadTimer = new ManualTimer(reloadTime);
            _cts = new CancellationTokenSource();
        }

        protected override void Start()
        {
            base.Start();

            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;

            if (projectileSpawnPoints.Count == 0)
                throw new UnityException("[Cannon] Projectile spawn points must be assigned.");
        }

        protected override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_reloadTimer == null) return;
            _reloadTimer.OnReady -= HandleReady;
            _reloadTimer.OnNotReady -= HandleNotReady;

            base.OnDestroy();
        }

        protected override void UpdateModule()
        {
            _reloadTimer.Progress(Time.deltaTime * ActualEfficiency);
        }

        public override void Shoot()
        {
            if (!_reloadTimer.IsReady) return;
            if (Ship == null) return;

            var targetPosition = Ship.AttackTargetPosition;

            var cannonOriginPosition = MathExt.AverageOfVectors(projectileSpawnPoints);

            if (!transform) return;
            var direction = (targetPosition - (Vector2)cannonOriginPosition).normalized;

            var angle = MathExt.AngleBetweenTwoPoints(targetPosition, cannonOriginPosition);

            var rotation = Quaternion.Euler(0, 0, angle - 90);

            var shooterColliders = Ship.OwnColliders;
            var bulletColliders = new List<Collider2D>();
            foreach (var projectileSpawnPoint in projectileSpawnPoints)
            {
                var newBullet =
                    _projectilesSpawner.Spawn(projectilePrefab, projectileSpawnPoint.position, rotation,
                        shooterColliders);

                var bulletCollider = newBullet.GetComponent<Collider2D>();
                foreach (var otherBulletCollider in bulletColliders)
                    Physics2D.IgnoreCollision(bulletCollider, otherBulletCollider);
                bulletColliders.Add(bulletCollider);

                var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
                // will be used in the future when proper cannon sprites will rotate 
                // bulletRigidbody.linearVelocity = PixelatedRigidbody.Rigidbody.linearVelocity;

                bulletRigidbody.AddForce(
                    direction * (projectileSpeed * GameplayConstants.CannonProjectileSpeedMultiplier),
                    ForceMode2D.Impulse);

                _shootingEventChannel?.Raise(new BulletShootingData(
                    Ship,
                    projectileSpawnPoint.position,
                    direction,
                    bulletRigidbody.mass * bulletRigidbody.linearVelocity.magnitude
                ));
            }

            _reloadTimer.Reset();
        }

        public override void StopShooting()
        {
        }

        public override bool IsReady()
        {
            return _reloadTimer.IsReady;
        }

        public override Sprite GetSprite()
        {
            return sprite;
        }

        protected override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new CannonModuleData
            {
                reloadTime = reloadTime,
                projectileSpeed = projectileSpeed,
                projectileLocalSpawnPoints =
                    projectileSpawnPoints.AsValueEnumerable().Select(p => (Vector2)p.localPosition).ToArray()
            };

            if (contentCatalog != null && projectilePrefab &&
                contentCatalog.TryGetContentId(projectilePrefab, out var projectileContentId))
                data.projectileContentId = projectileContentId;

            if (contentCatalog != null && sprite &&
                contentCatalog.TryGetSpriteContentId(sprite, out var spriteContentId))
                data.spriteContentId = spriteContentId;

            return JsonUtility.ToJson(data);
        }

        protected override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                return;

            var data = JsonUtility.FromJson<CannonModuleData>(typePayloadJson);
            if (data == null)
                return;

            reloadTime = data.reloadTime;
            projectileSpeed = data.projectileSpeed;
            _reloadTimer = new ManualTimer(reloadTime);

            foreach (var projectileSpawnPoint in projectileSpawnPoints) Destroy(projectileSpawnPoint.gameObject);
            projectileSpawnPoints.Clear();
            foreach (var projectileLocalSpawnPoint in data.projectileLocalSpawnPoints)
            {
                var go = new GameObject("Projectile Spawn Point");
                go.transform.SetParent(transform);
                go.transform.localPosition = projectileLocalSpawnPoint;

                projectileSpawnPoints.Add(go.transform);
            }

            if (contentCatalog != null &&
                contentCatalog.TryGetPrefab(data.projectileContentId, out var projectilePrefabValue))
                projectilePrefab = projectilePrefabValue;

            if (contentCatalog != null &&
                contentCatalog.TryGetSprite(data.spriteContentId, out var spriteValue))
                sprite = spriteValue;
        }

        public override float GetEnergyDraw()
        {
            return base.GetEnergyDraw() * (IsReady()
                ? 0.25f
                : 1f);
        }

#if UNITY_INCLUDE_TESTS
        internal void SetupForTesting(GameObject newProjectilePrefab,
            float newProjectileSpeed,
            float newReloadTime,
            Sprite newSprite,
            List<Transform> newProjectileSpawnPoints)
        {
            projectilePrefab = newProjectilePrefab;
            projectileSpeed = newProjectileSpeed;
            reloadTime = newReloadTime;
            sprite = newSprite;
            projectileSpawnPoints = newProjectileSpawnPoints;
        }

        internal GameObject InternalProjectilePrefab
        {
            get => projectilePrefab;
            set => projectilePrefab = value;
        }

        internal float InternalProjectileSpeed
        {
            get => projectileSpeed;
            set => projectileSpeed = value;
        }

        internal float InternalReloadTime
        {
            get => reloadTime;
            set
            {
                reloadTime = value;
                _reloadTimer = new ManualTimer(reloadTime);
            }
        }

        internal Sprite InternalSprite
        {
            get => sprite;
            set => sprite = value;
        }
#endif
    }
}