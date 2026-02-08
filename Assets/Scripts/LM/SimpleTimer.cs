using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace LM
{
    public class SimpleTimer : ITimer
    {
        private readonly float _interval;

        public SimpleTimer(float interval, bool startReady = true)
        {
            _interval = interval;
            IsReady = startReady;
        }

        public bool IsReady { get; private set; }

        public event Action OnReady;
        public event Action OnNotReady;

        public async UniTask Wait(float? seconds = null, CancellationToken cancellationToken = default)
        {
            OnNotReady?.Invoke();
            var elapsedSeconds = seconds ?? _interval;

            IsReady = false;

            try
            {
                await UniTask.Delay((int)(elapsedSeconds * 1000), cancellationToken: cancellationToken);
                IsReady = true;
                OnReady?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}