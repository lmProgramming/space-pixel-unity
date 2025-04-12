using UnityEngine;
using UnityEngine.Pool;

namespace EasyPool
{
    public interface IReturnToPool<T> where T : Component
    {
        void Initialize(IObjectPool<T> pool);
        void ResetState();
    }
}