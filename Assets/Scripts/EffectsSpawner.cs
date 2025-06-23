using EasyPool;
using UnityEngine;

public class EffectsSpawner : MonoBehaviour
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
        var explosion = _explosionPool.Get();
        explosion.transform.position = position;
        explosion.Play();
    }
}