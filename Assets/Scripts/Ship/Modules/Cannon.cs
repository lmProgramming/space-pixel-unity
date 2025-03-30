using Cysharp.Threading.Tasks;
using LM;
using UnityEngine;

namespace Ship.Modules
{
    public class Cannon : Module, IWeapon
    {
        [SerializeField] private GameObject projectilePrefab;

        [SerializeField] private float projectileSpeed;

        [SerializeField] private float reloadTime;

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

            var rotation = Quaternion.Euler(0, 0, angle - 90);

            var newBullet =
                ProjectilesSpawner.Instance.Spawn(projectilePrefab, transform.position, rotation, gameObject.layer);

            var bulletRigidbody = newBullet.GetComponent<Rigidbody2D>();
            bulletRigidbody.linearVelocity = PixelatedRigidbody.Rigidbody.linearVelocity;
            bulletRigidbody.AddForce(PixelatedRigidbody.Rigidbody.linearVelocity + direction * projectileSpeed,
                ForceMode2D.Impulse);

            _reloadTimer.Wait(reloadTime).Forget();
        }
    }
}