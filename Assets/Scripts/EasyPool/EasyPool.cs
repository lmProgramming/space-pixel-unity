using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace EasyPool
{
    [Serializable]
    public class EasyPool<T> where T : Component
    {
        public enum PoolType
        {
            Stack,
            LinkedList
        }

        private readonly bool _collectionChecks;
        private readonly int _maxPoolSize;
        private readonly PoolType _poolType;
        private readonly GameObject _prefab;

        private IObjectPool<T> _pool;

        public EasyPool(GameObject prefab, PoolType poolType = PoolType.Stack, bool collectionChecks = true,
            int maxPoolSize = 100)
        {
            _prefab = prefab;
            _poolType = poolType;
            _collectionChecks = collectionChecks;
            _maxPoolSize = maxPoolSize;
        }

        public IObjectPool<T> Pool => _pool ??= CreatePool();

        private IObjectPool<T> CreatePool()
        {
            return _poolType switch
            {
                PoolType.Stack => new ObjectPool<T>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool,
                    OnDestroyPoolObject,
                    _collectionChecks, 10, _maxPoolSize),
                PoolType.LinkedList => new LinkedPool<T>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool,
                    OnDestroyPoolObject,
                    _collectionChecks, _maxPoolSize),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private T CreatePooledItem()
        {
            var go = Object.Instantiate(_prefab);

            IReturnToPool<T> returnToPool = null;

            if (typeof(T) == typeof(ParticleSystem))
                returnToPool = (IReturnToPool<T>)go.AddComponent<ReturnToPoolParticleSystem>();

            returnToPool?.Initialize(Pool);

            return go.GetComponent<T>();
        }

        private static void OnReturnedToPool(T system)
        {
            system.gameObject.SetActive(false);
        }

        private static void OnTakeFromPool(T system)
        {
            system.gameObject.SetActive(true);
        }

        private static void OnDestroyPoolObject(T system)
        {
            if (system != null && system.gameObject != null) Object.Destroy(system.gameObject);
        }
    }
}