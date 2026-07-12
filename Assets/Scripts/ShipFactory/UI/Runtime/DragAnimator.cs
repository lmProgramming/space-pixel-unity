using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using ShipFactory.Helpers;
using UnityEngine;

namespace ShipFactory.UI.Runtime
{
    public class DragAnimator : IDisposable
    {
        private readonly Camera _cam;
        private readonly OverlayManager _overlayManager;

        private CancellationTokenSource _animationCts;
        private int _animationRunId;

        public DragAnimator(OverlayManager overlayManager)
        {
            _cam = Camera.main;
            _overlayManager = overlayManager;
        }

        public void Dispose()
        {
            _animationRunId++;
            _animationCts?.Cancel();
            _animationCts?.Dispose();
            _animationCts = null;
        }

        public Vector2 CalculateOffScreenBottomPosition(float worldX)
        {
            if (!_cam) return Snapper.SnapToGrid(new Vector2(worldX, -100f));

            var viewportBottom = _cam.ViewportToWorldPoint(new Vector3(0.5f, -0.15f, _cam.nearClipPlane));
            return Snapper.SnapToGrid(new Vector2(worldX, viewportBottom.y));
        }

        public void AnimateBundleMovement(ShipModuleSOInstanceBundle bundle, Vector2 from, Vector2 to,
            Action onComplete)
        {
            _animationRunId++;
            _animationCts?.Cancel();

            var cts = new CancellationTokenSource();
            _animationCts = cts;
            var runId = _animationRunId;

            AnimateBundleMovementAsync(bundle, from, to, onComplete, cts, runId).Forget();
        }

        private async UniTask AnimateBundleMovementAsync(ShipModuleSOInstanceBundle bundle,
            Vector2 from, Vector2 to, Action onComplete, CancellationTokenSource cts, int runId)
        {
            try
            {
                var token = cts.Token;
                const float duration = 0.22f;
                var elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    var eased = 1f - Mathf.Pow(1f - t, 3f);
                    var world = Vector2.Lerp(from, to, eased);

                    _overlayManager.SetPosition(bundle, world);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }

                _overlayManager.SetPosition(bundle, to);
                onComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer animation or Dispose.
            }
            finally
            {
                if (_animationRunId == runId)
                    _animationCts = null;

                cts.Dispose();
            }
        }
    }
}