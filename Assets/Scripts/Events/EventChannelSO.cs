using System;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
    public class EventChannelSO
    {
        private readonly EventChannelCore _core = new();
        [SerializeField] private UnityEvent unityEvent;

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

    public abstract class EventChannelSO<T> :
        ScriptableObject, IEventChannel<T>
    {
        [SerializeField] private UnityEvent<T> unityEvent;
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