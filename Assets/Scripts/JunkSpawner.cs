using EasyPool;
using Pixelation;
using UnityEngine;

public sealed class JunkSpawner : MonoBehaviour
{
    [SerializeField] private GameObject junkPrefab;

    [SerializeField] private Transform parent;

    [SerializeField] private Instantiator instantiator;

    public static JunkSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnJunk(Vector2 position, Quaternion rotation, Color32[,] colors, PixelatedRigidbody parentBody)
    {
        var newJunk = instantiator.Instantiate(junkPrefab, position, rotation, parent);

        var pixelatedJunk = newJunk.GetComponent<PixelatedJunk>();

        pixelatedJunk.Setup(colors);
        pixelatedJunk.CopyVelocity(parentBody);
    }
}