using System;
using System.Threading;
using Core.Gameplay.Combat;
using Core.Services;
using Core.Ship;
using Core.Ship.ModuleSnapshotPayloads;
using Cysharp.Threading.Tasks;
using LMPro;
using Pixelation;
using UnityEngine;
using ZLinq;

namespace Ships.Modules
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeam : Module, IWeapon
    {
        private const float ReloadEnergyMultiplier = 0.5f;
        private const float FiringEnergyMultiplier = 2f;

        [Header("Laser Settings")]
        [SerializeField]
        private LineRenderer lineRenderer;

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

        private ManualTimer _reloadTimer;
        public override ModuleType Type => ModuleType.Weapon;

        protected override void Awake()
        {
            base.Awake();

            Type = ModuleType.Weapon;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogError($"LaserBeam on {gameObject.name} requires a LineRenderer component.", this);
                enabled = false;
                return;
            }

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;

            _reloadTimer = new ManualTimer(reloadTime);
            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;
        }

        private void Update()
        {
            _reloadTimer.Progress(Time.deltaTime * ShipModuleEfficiency);
        }

        private void OnDestroy()
        {
            StopFiringCleanup();

            if (_reloadTimer == null) return;
            _reloadTimer.OnReady -= HandleReady;
            _reloadTimer.OnNotReady -= HandleNotReady;
        }

        public event Action OnReady;
        public event Action OnNotReady;

        public void Shoot()
        {
            StartShooting();
        }

        public bool IsReady()
        {
            return !_isFiring && _reloadTimer is { IsReady: true };
        }

        public Sprite GetSprite()
        {
            return sprite;
        }

        public void StopShooting()
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

            if (contentCatalog != null && sprite != null &&
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
            if (!lineRenderer) return;

            _isFiring = true;
            lineRenderer.enabled = true;
            HandleNotReady();

            _fireCts?.Cancel();
            _fireCts?.Dispose();
            _fireCts = new CancellationTokenSource();

            FireBeamUpdateAsync(_fireCts.Token).Forget();
        }

        private void StopFiringCleanup()
        {
            _isFiring = false;

            if (lineRenderer)
                lineRenderer.enabled = false;

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

        private async UniTask FireBeamUpdateAsync(CancellationToken token)
        {
            var timeRemaining = maxFireDuration * ShipModuleEfficiency;
            try
            {
                while (_isFiring && !token.IsCancellationRequested && timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;

                    var attackPosition = Ship.AttackTargetPosition;
                    Vector2 origin = originPoint.position;
                    var direction = (attackPosition - origin).normalized;
                    var endPoint = origin + direction * beamRange;

                    lineRenderer.SetPosition(0, origin);

                    var hit = RaycastIgnoringOwnColliders(origin, direction);

                    if (hit.collider)
                    {
                        lineRenderer.SetPosition(1, hit.point);

                        var pixelatedRigidbody = hit.collider.GetComponent<PixelatedRigidbody>();

                        var closestPixelPosition = pixelatedRigidbody.CollisionHandler.GetClosestPixelPosition(
                            pixelatedRigidbody.WorldToLocalPoint(hit.point));

                        if (closestPixelPosition.HasValue)
                            pixelatedRigidbody.DamagePixelAt(closestPixelPosition.Value,
                                damagePerSecond * Time.deltaTime, true);
                    }
                    else
                    {
                        lineRenderer.SetPosition(1, endPoint);
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

        private void HandleReady()
        {
            if (!_isFiring) OnReady?.Invoke();
        }

        private void HandleNotReady()
        {
            OnNotReady?.Invoke();
        }
    }
}