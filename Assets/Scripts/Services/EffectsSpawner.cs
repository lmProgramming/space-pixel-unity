using System.Runtime.CompilerServices;
using Core.Services;
using EasyPool;
using Instantiation;
using UnityEngine;

[assembly: InternalsVisibleTo("E2E")]

namespace Services
{
    public class EffectsSpawner : MonoBehaviour, IEffectsSpawner
    {
        [SerializeField] private GameObject explosionPrefab;

        [SerializeField] private Transform effectsHolder;
        [SerializeField] private Instantiator instantiator;

        private EasyPool<ParticleSystem> _explosionPool;

        private void Awake()
        {
            _explosionPool = new EasyPool<ParticleSystem>(explosionPrefab, effectsHolder, instantiator);
        }

        public void SpawnExplosion(Vector2 position)
        {
            if (!effectsHolder.gameObject.activeInHierarchy) return;
            var explosion = _explosionPool.Get();
            var position3 = new Vector3(position.x, position.y, explosion.transform.position.z);
            explosion.transform.position = position3;
            explosion.Play();
        }

#if UNITY_INCLUDE_TESTS
        internal void SetupForTesting(GameObject newExplosionPrefab, Transform newEffectsHolder,
            Instantiator newInstantiator)
        {
            explosionPrefab = newExplosionPrefab;
            effectsHolder = newEffectsHolder;
            instantiator = newInstantiator;
        }
#endif
    }
}