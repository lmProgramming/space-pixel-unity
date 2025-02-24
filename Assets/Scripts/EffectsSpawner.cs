using UnityEngine;

public sealed class EffectsSpawner : MonoBehaviour
{
    [SerializeField] private GameObject explosionEffectPrefab;

    public static EffectsSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnExplosion(Vector2 position)
    {
        Instantiate(explosionEffectPrefab, position, Quaternion.identity);
    }
}