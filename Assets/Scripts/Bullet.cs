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

    private void OnDestroy()
    {
        var results = new Collider2D[5];
        Physics2D.OverlapCircle(transform.position, 10f, new ContactFilter2D(), results);

        foreach (var result in results)
        {
            Debug.Log(result);
            result?.attachedRigidbody.AddForce((result.transform.position - transform.position) * 10,
                ForceMode2D.Impulse);
        }
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