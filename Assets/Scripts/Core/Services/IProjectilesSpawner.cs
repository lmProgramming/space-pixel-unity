using UnityEngine;

namespace Core.Services
{
    public interface IProjectilesSpawner
    {
        GameObject Spawn(GameObject projectilePrefab, Vector3 transformPosition, Quaternion rotation,
            Collider2D[] collidersToIgnore);
    }
}