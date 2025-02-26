using EasyPool;
using UnityEngine;

public sealed class EffectsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject explosionPrefab;
    private EasyPool<ParticleSystem> _explosionPool;

    public static EffectsSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _explosionPool = new EasyPool<ParticleSystem>(explosionPrefab);
    }

    public void SpawnExplosion(Vector2 position)
    {
        var explosion = _explosionPool.Pool.Get();
        explosion.transform.position = position;
        explosion.Play();
    }
}