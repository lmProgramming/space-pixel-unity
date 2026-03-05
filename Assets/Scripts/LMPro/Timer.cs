using System;

namespace LMPro
{
    public interface ITimer
    {
        public bool IsReady { get; }

        public event Action OnReady;
        public event Action OnNotReady;
    }
}