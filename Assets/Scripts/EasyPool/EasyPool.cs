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

        private readonly Instantiator _instantiator;
        private readonly int _maxPoolSize;
        private readonly Transform _parent;
        private readonly PoolType _poolType;
        private readonly GameObject _prefab;

        private IObjectPool<T> _pool;

        public EasyPool(GameObject prefab, Transform parent, Instantiator instantiator,
            PoolType poolType = PoolType.Stack, bool collectionChecks = true, int maxPoolSize = 100)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (instantiator == null) throw new ArgumentNullException(nameof(instantiator));

            _prefab = prefab;
            _parent = parent;
            _instantiator = instantiator;
            _poolType = poolType;
            _collectionChecks = collectionChecks;
            _maxPoolSize = maxPoolSize;
        }

        private IObjectPool<T> Pool => _pool ??= CreatePool();
        public int CountInactive => Pool.CountInactive;

        public T Get()
        {
            return Pool.Get();
        }

        public void Release(T element)
        {
            Pool.Release(element);
        }

        public void Clear()
        {
            Pool.Clear();
        }

        private IObjectPool<T> CreatePool()
        {
            return _poolType switch
            {
                PoolType.Stack => new ObjectPool<T>(
                    CreatePooledItem,
                    OnTakeFromPool,
                    OnReturnedToPool,
                    OnDestroyPoolObject,
                    _collectionChecks,
                    10,
                    _maxPoolSize),
                PoolType.LinkedList => new LinkedPool<T>(
                    CreatePooledItem,
                    OnTakeFromPool,
                    OnReturnedToPool,
                    OnDestroyPoolObject,
                    _collectionChecks,
                    _maxPoolSize),
                _ => throw new ArgumentOutOfRangeException(nameof(_poolType), _poolType, null)
            };
        }

        private T CreatePooledItem()
        {
            var go = _instantiator.Instantiate(_prefab, _parent, false);

            var mainComponent = go.GetComponent<T>();
            if (mainComponent == null)
            {
                Debug.LogError(
                    $"Prefab '{_prefab.name}' does not contain the required component '{typeof(T).Name}'. Destroying instance.",
                    go);
                Object.Destroy(go);
                throw new InvalidOperationException($"Prefab missing component {typeof(T).Name}");
            }

            var returnToPoolComponent = go.GetComponent<IReturnToPool<T>>();
            if (returnToPoolComponent != null)
                returnToPoolComponent.Initialize(Pool);
            else
                Debug.LogWarning(
                    $"Prefab '{_prefab.name}' or its component '{typeof(T).Name}' does not implement IReturnToPool<{typeof(T).Name}>. Object will need manual releasing.",
                    go);

            return mainComponent;
        }

        private void OnReturnedToPool(T system)
        {
            if (system is IReturnToPool<T> returner) returner.ResetState();
            if (system != null) system.gameObject.SetActive(false);
        }

        private void OnTakeFromPool(T system)
        {
            if (system != null) system.gameObject.SetActive(true);
        }

        private void OnDestroyPoolObject(T system)
        {
            if (system != null && system.gameObject != null) Object.Destroy(system.gameObject);
        }
    }
}