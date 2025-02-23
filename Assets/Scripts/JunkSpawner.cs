using UnityEngine;

public sealed class JunkSpawner : MonoBehaviour
{
    [SerializeField] private GameObject junkPrefab;

    [SerializeField] private Transform parent;

    public static JunkSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnJunk(Vector2 position, Quaternion rotation, Color[,] colors)
    {
        var newJunk = Instantiate(junkPrefab, position, rotation, parent);

        newJunk.GetComponent<PixelatedJunk>().Setup(colors);
    }
}