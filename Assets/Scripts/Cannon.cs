using Other;
using Pixelation;
using Ship;
using UnityEngine;

public class Cannon : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private Transform projectilesHolder;

    [SerializeField] private float projectileSpeed;

    private PixelatedRigidbody _parentBody;

    public void Shoot()
    {
        var pointerPosition = GameInput.WorldPointerPosition;

        var direction = (pointerPosition - (Vector2)transform.position).normalized;

        var newBullet = Instantiate(projectilePrefab, transform.position, transform.rotation, projectilesHolder);

        var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
        bulletRigidbody.linearVelocity = _parentBody.Rigidbody.linearVelocity;
        bulletRigidbody.AddForce(_parentBody.Rigidbody.linearVelocity + direction * projectileSpeed,
            ForceMode2D.Impulse);
    }

    public void SetBody(ShipBody body)
    {
        _parentBody = body;
    }
}