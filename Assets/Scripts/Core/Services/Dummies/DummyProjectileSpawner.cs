using System;
using UnityEngine;

namespace Core.Services.Dummies
{
    public class DummyProjectileSpawner : IProjectilesSpawner
    {
        public GameObject Spawn(GameObject projectilePrefab, Vector3 transformPosition, Quaternion rotation,
            Collider2D[] collidersToIgnore)
        {
            throw new NotSupportedException();
        }
    }
}