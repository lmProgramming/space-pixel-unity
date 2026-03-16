using System;

namespace Events
{
    public interface IEventChannel
    {
        void Raise();
        void Register(Action listener);
        void Unregister(Action listener);
    }

    public interface IEventChannel<T>
    {
        void Raise(T data);
        void Register(Action<T> listener);
        void Unregister(Action<T> listener);
    }
}