using System;
using Cysharp.Threading.Tasks;
using Pixelation;
using UnityEngine;

public class Bullet : PixelatedRigidbody
{
    [SerializeField] private float fadeOutTime = 2f;
    [SerializeField] private float lifeTime = 2f;

    public override void Start()
    {
        base.Start();

        DelayedFadeOutAsync().Forget();
    }

    private async UniTaskVoid DelayedFadeOutAsync()
    {
        try
        {
            var token = this.GetCancellationTokenOnDestroy();

            await UniTask.Delay(TimeSpan.FromSeconds(lifeTime), cancellationToken: token);
            await FadeOutAndDestroy(fadeOutTime);
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