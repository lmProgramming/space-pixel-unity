using EasyPool;
using UnityEngine;

public class ProjectilesSpawner : MonoBehaviour
{
    [field: SerializeField] public Transform ProjectilesHolder { get; private set; }

    [SerializeField] private Instantiator instantiator;

    public GameObject Spawn(GameObject projectilePrefab, Vector3 transformPosition, Quaternion rotation,
        LayerMask layer)
    {
        var bulletObject = instantiator.Instantiate(projectilePrefab, transformPosition, rotation, ProjectilesHolder);
        bulletObject.GetComponent<Bullet>().SetLayer(layer);
        return bulletObject;
    }
}