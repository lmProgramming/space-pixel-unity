using UnityEngine;
using UnityEngine.Pool;

namespace EasyPool
{
    public class ReturnToPoolParticleSystem : MonoBehaviour, IReturnToPool<ParticleSystem>
    {
        private ParticleSystem _mainComponent;
        private IObjectPool<ParticleSystem> _pool;

        private void Start()
        {
            _mainComponent = GetComponent<ParticleSystem>();
        }

        private void OnParticleSystemStopped()
        {
            _pool.Release(_mainComponent);
        }

        public void Initialize(IObjectPool<ParticleSystem> pool)
        {
            _pool = pool;
        }

        public void OnConfigured()
        {
        }

        public void ResetState()
        {
        }
    }
}