using UnityEngine;

public sealed class ProjectilesSpawner : MonoBehaviour
{
    public static ProjectilesSpawner Instance;

    [field: SerializeField] public Transform ProjectilesHolder { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        Instance = null;
    }

    public GameObject Spawn(GameObject projectilePrefab, Vector3 transformPosition, Quaternion rotation,
        LayerMask layer)
    {
        var bulletObject = Instantiate(projectilePrefab, transformPosition, rotation, ProjectilesHolder);
        bulletObject.GetComponent<Bullet>().SetLayer(layer);
        return bulletObject;
    }
}