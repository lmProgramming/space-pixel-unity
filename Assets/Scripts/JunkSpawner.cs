using UnityEngine;

public sealed class JunkSpawner : MonoBehaviour
{
    [SerializeField] private GameObject junkPrefab;

    public static JunkSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnJunk(Vector2 position, Quaternion rotation, Color[,] colors)
    {
        var newJunk = Instantiate(junkPrefab, position, rotation, transform);

        newJunk.GetComponent<PixelatedJunk>().SetupFromColors(colors);
    }
}