using Core.Services;
using Gameplay.Combat;
using Instantiation;
using UnityEngine;

namespace Services
{
    public class ProjectilesSpawner : MonoBehaviour, IProjectilesSpawner
    {
        [field: SerializeField] public Transform ProjectilesHolder { get; private set; }

        [SerializeField] private Instantiator instantiator;

        public GameObject Spawn(GameObject projectilePrefab, Vector3 transformPosition, Quaternion rotation,
            Collider2D[] collidersToIgnore)
        {
            var bulletObject =
                instantiator.Instantiate(projectilePrefab, transformPosition, rotation, ProjectilesHolder);

            bulletObject.GetComponent<Bullet>().SetLayer(LayerMask.NameToLayer("Bullets"));

            IgnoreCollisionsBetweenBulletAndShooter(bulletObject, collidersToIgnore);

            return bulletObject;
        }

        private static void IgnoreCollisionsBetweenBulletAndShooter(GameObject bulletObject,
            Collider2D[] shooterColliders)
        {
            var bulletColliders = bulletObject.GetComponentsInChildren<Collider2D>();
            foreach (var bulletCollider in bulletColliders)
            foreach (var shooterCollider in shooterColliders)
                Physics2D.IgnoreCollision(bulletCollider, shooterCollider, true);
        }
    }
}