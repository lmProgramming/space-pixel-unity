using System;
using System.Threading;
using Core.Gameplay.Combat;
using Core.Ship;
using Cysharp.Threading.Tasks;
using LM;
using Pixelation;
using UnityEngine;
using ZLinq;
using Random = UnityEngine.Random;

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
        [SerializeField] private float pixelsDestroyedPerSecond = 10f;
        [SerializeField] private LayerMask hitLayers;

        [SerializeField] private GameObject icon;

        [SerializeField] private Transform originPoint;

        [Header("Weapon Base Settings")]
        [SerializeField]
        private float reloadTime = 2.0f;

        private CancellationTokenSource _fireCts;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[100];
        private bool _isFiring;

        private ManualTimer _reloadTimer;

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

        public GameObject GetIcon()
        {
            return icon;
        }

        public void StopShooting()
        {
            _isFiring = false;

            StopFiringCleanup();

            _reloadTimer?.Reset();
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

                    var attackPosition = GameInput.WorldPointerPosition;
                    Vector2 origin = originPoint.position;
                    var direction = (attackPosition - origin).normalized;
                    var endPoint = origin + direction * beamRange;

                    lineRenderer.SetPosition(0, origin);

                    var hit = RaycastIgnoringOwnColliders(origin, direction);

                    if (hit.collider)
                    {
                        lineRenderer.SetPosition(1, hit.point);

                        if (Random.value < pixelsDestroyedPerSecond * Time.deltaTime)
                        {
                            var pixelatedRigidbody = hit.collider.GetComponent<PixelatedRigidbody>();

                            var closestPixelPosition = pixelatedRigidbody.CollisionHandler.GetClosestPixelPosition(
                                pixelatedRigidbody.WorldToLocalPoint(hit.point));

                            if (closestPixelPosition.HasValue)
                                pixelatedRigidbody.RemovePixelAt(closestPixelPosition.Value, true);
                        }
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