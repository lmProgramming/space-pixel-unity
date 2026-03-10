using System;

namespace Events
{
    public class EventChannelCore
    {
        protected event Action OnEvent;

        public void Raise()
        {
            OnEvent?.Invoke();
        }

        public void Register(Action l)
        {
            OnEvent += l;
        }

        public void Unregister(Action l)
        {
            OnEvent -= l;
        }
    }

    public class EventChannelCore<T>
    {
        protected event Action<T> OnEvent;

        public void Raise(T data)
        {
            OnEvent?.Invoke(data);
        }

        public void Register(Action<T> l)
        {
            OnEvent += l;
        }

        public void Unregister(Action<T> l)
        {
            OnEvent -= l;
        }
    }
}