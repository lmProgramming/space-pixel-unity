using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace EasyPool
{
    [RequireComponent(typeof(AudioSource))]
    public class ReturnToPoolAudioSource : MonoBehaviour, IReturnToPool<AudioSource>
    {
        private CancellationTokenSource _cts;
        private AudioSource _mainComponent;
        private IObjectPool<AudioSource> _pool;

        private void Awake()
        {
            _mainComponent = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (!_mainComponent || !_mainComponent) return;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void OnDisable()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public void Initialize(IObjectPool<AudioSource> pool)
        {
            _pool = pool;
        }

        public void OnConfigured()
        {
            var duration = _mainComponent.Duration();

            if (!_mainComponent.loop && duration > 0) ReturnAfterDelayAsync(duration, _cts.Token).Forget();
        }

        public void ResetState()
        {
        }

        private async UniTaskVoid ReturnAfterDelayAsync(float delay, CancellationToken token)
        {
            try
            {
                if (delay < 0) delay = 0;

                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);

                if (this != null && gameObject != null && _pool != null && !token.IsCancellationRequested)
                    _pool.Release(_mainComponent);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to release pooled '{_mainComponent?.name ?? "Unknown"}': {e.Message}", this);
            }
            finally
            {
                if (_cts != null && _cts.Token == token)
                {
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }
    }
}