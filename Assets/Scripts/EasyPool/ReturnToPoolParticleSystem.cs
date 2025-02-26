using UnityEngine;
using UnityEngine.Pool;

namespace EasyPool
{
    public class ReturnToPoolParticleSystem : MonoBehaviour, IReturnToPool<ParticleSystem>
    {
        public ParticleSystem mainComponent;
        private IObjectPool<ParticleSystem> _pool;

        private void Start()
        {
            mainComponent = GetComponent<ParticleSystem>();
        }

        private void OnParticleSystemStopped()
        {
            _pool.Release(mainComponent);
        }

        public void Initialize(IObjectPool<ParticleSystem> pool)
        {
            _pool = pool;
        }
    }
}