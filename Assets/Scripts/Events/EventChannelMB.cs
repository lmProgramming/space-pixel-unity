using System;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
    public abstract class EventChannelMB : MonoBehaviour, IEventChannel
    {
        [SerializeField]
        private UnityEvent unityEvent;

        private readonly EventChannelCore _core = new();

        public void Raise()
        {
            _core.Raise();
            unityEvent?.Invoke();
        }

        public void Register(Action l)
        {
            _core.Register(l);
        }

        public void Unregister(Action l)
        {
            _core.Unregister(l);
        }
    }

    public abstract class EventChannelMB<T> : MonoBehaviour, IEventChannel<T>
    {
        [SerializeField]
        private UnityEvent<T> unityEvent;

        private readonly EventChannelCore<T> _core = new();

        public void Raise(T data)
        {
            _core.Raise(data);
            unityEvent?.Invoke(data);
        }

        public void Register(Action<T> l)
        {
            _core.Register(l);
        }

        public void Unregister(Action<T> l)
        {
            _core.Unregister(l);
        }
    }
}