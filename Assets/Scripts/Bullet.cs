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
            await UniTask.Delay(TimeSpan.FromSeconds(lifeTime));
            await FadeOutAndDestroy(fadeOutTime);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}