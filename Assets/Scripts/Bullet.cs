using System;
using Cysharp.Threading.Tasks;
using Pixelation;
using UnityEngine;

public class Bullet : PixelatedRigidbody
{
    private const float PushAwayRadius = 10f;
    private const float PushAwayStrength = 10f;

    [SerializeField] private float fadeOutTime = 2f;
    [SerializeField] private float lifeTime = 2f;

    private Collider2D _collider;

    public override void Start()
    {
        base.Start();

        _collider = GetComponent<Collider2D>();
        _collider.enabled = false;

        CheckCollisionsAndEnableCollider().Forget();

        DelayedFadeOutAsync().Forget();
    }

    private void OnDestroy()
    {
        var results = new Collider2D[5];
        Physics2D.OverlapCircle(transform.position, PushAwayRadius, new ContactFilter2D(), results);

        foreach (var result in results)
            result?.attachedRigidbody.AddForce((result.transform.position - transform.position) * PushAwayStrength,
                ForceMode2D.Impulse);
    }

    private async UniTaskVoid CheckCollisionsAndEnableCollider()
    {
        while (true)
        {
            var results = new Collider2D[1];
            var collisionCount = Physics2D.OverlapCircle(transform.position, 1f, new ContactFilter2D(), results);

            if (collisionCount == 0)
            {
                _collider.enabled = true;
                break;
            }

            await UniTask.DelayFrame(4);
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