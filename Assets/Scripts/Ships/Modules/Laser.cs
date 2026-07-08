using System;
using System.Threading;
using Core.Services;
using Core.Ships.Snapshots.Module;
using Core.Ships.Snapshots.Module.ModuleData;
using Cysharp.Threading.Tasks;
using LMPro;
using Pixelation;
using UnityEngine;
using ZLinq;

namespace Ships.Modules
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeam : WeaponBase
    {
        private const float ReloadEnergyMultiplier = 0.5f;
        private const float FiringEnergyMultiplier = 2f;

        [SerializeField] private float beamRange = 20f;

        [SerializeField] private float maxFireDuration = 1.5f;

        [SerializeField]
        private float damagePerSecond = 10f;

        [SerializeField] private LayerMask hitLayers;

        [SerializeField] private Sprite sprite;

        [SerializeField] private Transform originPoint;

        [Header("Weapon Base Settings")]
        [SerializeField]
        private float reloadTime = 2.0f;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[100];

        private CancellationTokenSource _fireCts;
        private bool _isFiring;

        [Header("Laser Settings")]
        private LineRenderer _lineRenderer;

        private ManualTimer _reloadTimer;

        protected override void Awake()
        {
            base.Awake();

            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                Debug.LogError($"LaserBeam on {gameObject.name} requires a LineRenderer component.", this);
                enabled = false;
                return;
            }

            _lineRenderer.positionCount = 2;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.enabled = false;

            _reloadTimer = new ManualTimer(reloadTime);
            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;
        }

        private void Update()
        {
            _reloadTimer.Progress(Time.deltaTime * ActualEfficiency);
        }

        protected override void OnDestroy()
        {
            StopFiringCleanup();

            if (_reloadTimer == null) return;
            _reloadTimer.OnReady -= HandleReady;
            _reloadTimer.OnNotReady -= HandleNotReady;

            base.OnDestroy();
        }

        public override void Shoot()
        {
            StartShooting();
        }

        public override bool IsReady()
        {
            return !_isFiring && _reloadTimer is { IsReady: true };
        }

        public override Sprite GetSprite()
        {
            return sprite;
        }

        public override void StopShooting()
        {
            _isFiring = false;

            StopFiringCleanup();

            _reloadTimer?.Reset();
        }

        public override string CaptureTypePayloadJson(IGameContentCatalog contentCatalog)
        {
            var data = new LaserBeamModuleData
            {
                reloadTime = reloadTime,
                beamRange = beamRange
            };

            if (contentCatalog != null && sprite &&
                contentCatalog.TryGetSpriteContentId(sprite, out var contentId))
                data.spriteContentId = contentId;

            return JsonUtility.ToJson(data);
        }

        public override void ApplyTypePayloadJson(string typePayloadJson, IGameContentCatalog contentCatalog)
        {
            if (string.IsNullOrWhiteSpace(typePayloadJson))
                return;

            var data = JsonUtility.FromJson<LaserBeamModuleData>(typePayloadJson);
            if (data == null)
                return;

            reloadTime = data.reloadTime;
            beamRange = data.beamRange;
            _reloadTimer = new ManualTimer(reloadTime);

            if (contentCatalog != null && contentCatalog.TryGetSprite(data.spriteContentId, out var spriteValue))
                sprite = spriteValue;
        }

        public override float GetEnergyDraw()
        {
            if (IsReady()) return 0;

            var baseEnergyDraw = base.GetEnergyDraw();
            var multiplier = _isFiring ? FiringEnergyMultiplier : ReloadEnergyMultiplier;
            return baseEnergyDraw * multiplier;
        }

        private void StartShooting()
        {
            if (!IsReady()) return;

            _isFiring = true;
            _lineRenderer.enabled = true;
            HandleNotReady();

            _fireCts?.Cancel();
            _fireCts?.Dispose();
            _fireCts = new CancellationTokenSource();

            FireBeamUpdateAsync(_fireCts.Token).Forget();
        }

        private void StopFiringCleanup()
        {
            _isFiring = false;

            if (_lineRenderer)
                _lineRenderer.enabled = false;

            _fireCts?.Cancel();
            _fireCts?.Dispose();
            _fireCts = null;
        }

        private RaycastHit2D RaycastIgnoringOwnColliders(Vector2 origin, Vector2 direction)
        {
            var size = Physics2D.RaycastNonAlloc(origin, direction, _hits, beamRange, hitLayers);
            var ownColliders = Ship?.OwnColliders;

            for (var index = 0; index < Mathf.Min(size, _hits.Length); index++)
            {
                var hit = _hits[index];
                if (ownColliders == null || !IsOwnCollider(hit.collider, ownColliders))
                    return hit;
            }

            return default;
        }

        private static bool IsOwnCollider(Collider2D collider, Collider2D[] ownColliders)
        {
            return ownColliders.AsValueEnumerable().Any(own => own == collider);
        }

        public override void RestoreFromSnapshot(ModuleSnapshot snapshot, IGameContentCatalog contentCatalog)
        {
            base.RestoreFromSnapshot(snapshot, contentCatalog);

            gameObject.AddComponent<LineRenderer>();
        }

        private async UniTask FireBeamUpdateAsync(CancellationToken token)
        {
            var timeRemaining = maxFireDuration * ActualEfficiency;
            try
            {
                while (_isFiring && !token.IsCancellationRequested && timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;

                    var attackPosition = Ship.AttackTargetPosition;
                    Vector2 origin = originPoint.position;
                    var direction = (attackPosition - origin).normalized;
                    var endPoint = origin + direction * beamRange;

                    _lineRenderer.SetPosition(0, origin);

                    var hit = RaycastIgnoringOwnColliders(origin, direction);

                    if (hit.collider)
                    {
                        _lineRenderer.SetPosition(1, hit.point);

                        var pixelatedRigidbody = hit.collider.GetComponent<PixelatedRigidbody>();

                        var closestPixelPosition = pixelatedRigidbody.CollisionHandler.GetClosestPixelPosition(
                            pixelatedRigidbody.WorldToLocalPoint(hit.point));

                        if (closestPixelPosition.HasValue)
                            pixelatedRigidbody.DamagePixelAt(closestPixelPosition.Value,
                                damagePerSecond * Time.deltaTime, true);
                    }
                    else
                    {
                        _lineRenderer.SetPosition(1, endPoint);
                    }

                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                StopShooting();
            }
        }

        protected override void HandleReady()
        {
            if (!_isFiring) base.HandleReady();
        }
    }
}