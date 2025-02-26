using UnityEngine.Pool;

namespace EasyPool
{
    public interface IReturnToPool<T> where T : class
    {
        public void Initialize(IObjectPool<T> pool);
    }
}