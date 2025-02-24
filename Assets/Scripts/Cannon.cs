using Other;
using UnityEngine;

public class Cannon : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private Transform projectileParent;

    [SerializeField] private float projectileSpeed;

    public void Shoot()
    {
        var pointerPosition = GameInput.WorldPointerPosition;

        var direction = (pointerPosition - (Vector2)transform.position).normalized;

        var newBullet = Instantiate(projectilePrefab, transform.position, transform.rotation, projectileParent);

        newBullet.GetComponent<Rigidbody2D>().AddForce(direction * projectileSpeed);
    }
}