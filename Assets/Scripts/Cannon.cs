using UnityEngine;

public class Cannon : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private Transform projectileParent;

    [SerializeField] private float projectileSpeed;

    public void Shoot()
    {
        var newBullet = Instantiate(projectilePrefab, transform.position, transform.rotation, projectileParent);

        newBullet.GetComponent<Rigidbody2D>().AddForce(transform.up * projectileSpeed);
    }
}