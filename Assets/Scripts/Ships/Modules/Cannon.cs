using System.Runtime.CompilerServices;
using System.Threading;
using Core.Services;
using Core.Ship.ModuleSnapshotPayloads;
using LMPro;
using UnityEngine;
using Zenject;

[assembly: InternalsVisibleTo("E2E")]
[assembly: InternalsVisibleTo("Ships.Tests")]

namespace Ships.Modules
{
    public class Cannon : WeaponBase
    {
        [SerializeField] private GameObject projectilePrefab;

        [SerializeField] private float projectileSpeed;

        [SerializeField] private float reloadTime;

        [SerializeField] private Sprite sprite;
        private CancellationTokenSource _cts;

        [Inject] private IProjectilesSpawner _projectilesSpawner;

        private ManualTimer _reloadTimer;

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
        }

        public void Update()
        {
            _reloadTimer.Progress(Time.deltaTime * ShipModuleEfficiency);
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

        public override void Shoot()
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

        public override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new CannonModuleData
            {
                reloadTime = reloadTime,
                projectileSpeed = projectileSpeed
            };

            if (contentCatalog != null && projectilePrefab &&
                contentCatalog.TryGetContentId(projectilePrefab, out var projectileContentId))
                data.projectileContentId = projectileContentId;

            if (contentCatalog != null && sprite &&
                contentCatalog.TryGetSpriteContentId(sprite, out var spriteContentId))
                data.spriteContentId = spriteContentId;

            return JsonUtility.ToJson(data);
        }

        public override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                return;

            var data = JsonUtility.FromJson<CannonModuleData>(typePayloadJson);
            if (data == null)
                return;

            reloadTime = data.reloadTime;
            projectileSpeed = data.projectileSpeed;
            _reloadTimer = new ManualTimer(reloadTime);

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