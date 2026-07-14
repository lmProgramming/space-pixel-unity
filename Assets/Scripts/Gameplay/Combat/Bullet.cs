using System;
using System.Collections.Generic;
using Core.Constants;
using Core.Pixelation;
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
        }

        private void OnEnable()
        {
            OnPixelsLost += OnOnPixelsLost;

            Destroyed += HandleDestroy;
        }

        private void OnDisable()
        {
            OnPixelsLost -= OnOnPixelsLost;

            Destroyed -= HandleDestroy;
        }

        private void OnOnPixelsLost(List<Vector2Int> vector2Ints, PixelLoseReason pixelLoseReason)
        {
            SetLayer(PhysicsLayers.Default);
        }

        private void HandleDestroy(IPixelated obj)
        {
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

                await UniTask.Delay(
                    TimeSpan.FromSeconds(LifeTime * GameplayConstants.CannonProjectileLifetimeMultiplier),
                    cancellationToken: token);
                await FadeOutAndDestroy(FadeOutTime * GameplayConstants.CannonProjectileLifetimeMultiplier);
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