using System;
using Cysharp.Threading.Tasks;

namespace LM
{
    public class SimpleTimer
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

        public async UniTask Wait(float? seconds = null)
        {
            OnNotReady?.Invoke();
            var elapsedSeconds = seconds ?? _interval;

            IsReady = false;
            await UniTask.Delay((int)(elapsedSeconds * 1000));
            IsReady = true;
            OnReady?.Invoke();
        }
    }
}