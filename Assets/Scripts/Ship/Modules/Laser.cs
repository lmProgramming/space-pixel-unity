using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LM;
using Pixelation;
using UnityEngine;

namespace Ship.Modules
{
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeam : Module, IWeapon // Changed interface if desired
    {
        [Header("Laser Settings")] [SerializeField]
        private LineRenderer lineRenderer;

        [SerializeField] private float beamRange = 20f;

        [SerializeField] private float maxFireDuration = 1.5f; // No longer fixed duration
        [SerializeField] private float damagePerSecond = 10f;
        [SerializeField] private LayerMask hitLayers;

        [Header("Weapon Base Settings")] [SerializeField]
        private float reloadTime = 2.0f;

        private CancellationTokenSource _fireCts;
        private bool _isFiring;

        private SimpleTimer _reloadTimer;

        private void Awake()
        {
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

            _reloadTimer = new SimpleTimer(reloadTime);
            _reloadTimer.OnReady += HandleReady;
            _reloadTimer.OnNotReady += HandleNotReady;
        }

        private void OnDestroy()
        {
            StopFiringCleanup(); // Ensure cleanup on destroy

            if (_reloadTimer != null)
            {
                _reloadTimer.OnReady -= HandleReady;
                _reloadTimer.OnNotReady -= HandleNotReady;
            }
        }

        public event Action OnReady;
        public event Action OnNotReady;

        public void Shoot()
        {
            StartShooting();
        }

        public bool IsReady()
        {
            return !_isFiring && _reloadTimer != null && _reloadTimer.IsReady;
        }

        public GameObject GetIcon() // Keep if IWeapon still needs it
        {
            // return icon; // Return actual icon if you re-add the field
            return null;
        }

        // Renamed from Shoot to reflect starting the action
        public void StartShooting()
        {
            if (!IsReady()) return;

            _isFiring = true;
            lineRenderer.enabled = true;
            HandleNotReady(); // Signal busy state

            _fireCts?.Cancel();
            _fireCts?.Dispose();
            _fireCts = new CancellationTokenSource();

            FireBeamUpdateAsync(_fireCts.Token).Forget();
        }

        // New method to stop firing when input is released
        public void StopShooting()
        {
            if (!_isFiring) return; // Only stop if currently firing

            StopFiringCleanup();

            // Start reload timer after stopping
            if (_reloadTimer != null) _reloadTimer.Wait(reloadTime).Forget();
            // HandleNotReady will be invoked by the timer starting its wait
        }

        private void StopFiringCleanup()
        {
            _isFiring = false;
            if (lineRenderer != null) // Check if lineRenderer still exists
                lineRenderer.enabled = false;
            _fireCts?.Cancel();
            _fireCts?.Dispose();
            _fireCts = null;
        }


        private async UniTask FireBeamUpdateAsync(CancellationToken token)
        {
            var timeRemaining = maxFireDuration;
            try
            {
                // Loop runs as long as _isFiring is true and not cancelled
                while (_isFiring && !token.IsCancellationRequested && timeRemaining > 0)
                {
                    timeRemaining -= Time.deltaTime;

                    var pointerPosition = GameInput.WorldPointerPosition;
                    Vector2 origin = transform.position;
                    var direction = (pointerPosition - origin).normalized;
                    var endPoint = origin + direction * beamRange;

                    lineRenderer.SetPosition(0, origin);

                    var hit = Physics2D.Raycast(origin, direction, beamRange, hitLayers);

                    if (hit.collider != null)
                    {
                        lineRenderer.SetPosition(1, hit.point);
                        var pixelatedRigidbody = hit.collider.GetComponent<PixelatedRigidbody>();

                        var closestPixelPosition = pixelatedRigidbody.CollisionHandler.GetClosestPixelPosition(
                            pixelatedRigidbody.WorldToLocalPoint(endPoint));

                        if (closestPixelPosition.HasValue) pixelatedRigidbody.RemovePixelAt(closestPixelPosition.Value);
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
                // Expected when firing stops, ignore
            }
            finally
            {
                // Final cleanup is handled by StopFiringCleanup or OnDestroy
                if (lineRenderer != null) // Ensure line is off if loop exits unexpectedly
                    lineRenderer.enabled = false;
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