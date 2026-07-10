using System;

namespace LMPro
{
    public class ManualTimer : ITimer
    {
        private readonly float _interval;
        private float _timeLeftToReady;

        public ManualTimer(float interval, bool startReady = true)
        {
            _interval = interval;
            IsReady = startReady;
        }

        public bool IsReady { get; private set; }

        public event Action OnReady;
        public event Action OnNotReady;

        public void Reset(float? newInterval = null)
        {
            var actualInterval = newInterval ?? _interval;
            _timeLeftToReady = actualInterval;
            IsReady = false;

            OnNotReady?.Invoke();
        }

        public void Progress(float deltaTime)
        {
            if (IsReady) return;

            _timeLeftToReady -= deltaTime;

            if (_timeLeftToReady > 0) return;

            IsReady = true;
            OnReady?.Invoke();
        }
    }
}