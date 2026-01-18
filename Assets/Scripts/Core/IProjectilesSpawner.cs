using UnityEngine;

namespace Core
{
    public interface IProjectilesSpawner
    {
        GameObject Spawn(GameObject projectilePrefab, Vector3 transformPosition, Quaternion rotation,
            LayerMask layer);
    }
}