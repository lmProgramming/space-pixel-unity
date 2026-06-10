using System;
using Core.Constants;
using Cysharp.Threading.Tasks;
using Pixelation;
using UnityEngine;

namespace Gameplay.Combat
{
    public class Bullet : PixelatedRigidbody
    {
        private const float PushAwayRadius = 10f;
        private const float PushAwayStrength = 10f;

        private const float FadeOutTime = 2f;
        private const float LifeTime = 2f;

        protected override void Awake()
        {
            base.Awake();

            DelayedFadeOutAsync().Forget();

            OnPixelsLost += (_, _) => SetLayer(PhysicsLayers.Default);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            var results = new Collider2D[5];
            Physics2D.OverlapCircle(transform.position, PushAwayRadius, ContactFilter2D.noFilter, results);

            foreach (var result in results)
                result?.attachedRigidbody.AddForce((result.transform.position - transform.position) * PushAwayStrength,
                    ForceMode2D.Impulse);
        }

        public void SetLayer(LayerMask layer)
        {
            gameObject.layer = layer;
        }

        private async UniTaskVoid DelayedFadeOutAsync()
        {
            try
            {
                var token = this.GetCancellationTokenOnDestroy();

                await UniTask.Delay(TimeSpan.FromSeconds(LifeTime), cancellationToken: token);
                await FadeOutAndDestroy(FadeOutTime);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}