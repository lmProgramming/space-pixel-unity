using Cysharp.Threading.Tasks;
using LM;
using Pixelation;
using Ship;
using UnityEngine;

public class Cannon : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject projectilePrefab;

    [SerializeField] private Transform projectilesHolder;

    [SerializeField] private float projectileSpeed;

    [SerializeField] private float reloadTime;

    private PixelatedRigidbody _parentBody;

    private SimpleTimer _reloadTimer;

    private void Start()
    {
        _reloadTimer = new SimpleTimer(reloadTime);
    }

    public void Shoot()
    {
        if (!_reloadTimer.IsReady) return;

        var pointerPosition = GameInput.WorldPointerPosition;

        var direction = (pointerPosition - (Vector2)transform.position).normalized;

        var angle = MathExt.AngleBetweenTwoPoints(pointerPosition, transform.position);

        var rotation = Quaternion.Euler(0, 0, angle + 90);

        var newBullet = Instantiate(projectilePrefab, transform.position, rotation, projectilesHolder);

        var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
        bulletRigidbody.linearVelocity = _parentBody.Rigidbody.linearVelocity;
        bulletRigidbody.AddForce(_parentBody.Rigidbody.linearVelocity + direction * projectileSpeed,
            ForceMode2D.Impulse);

        _reloadTimer.Wait(reloadTime).Forget();
    }

    public void SetBody(ShipBody body)
    {
        _parentBody = body;
    }
}
