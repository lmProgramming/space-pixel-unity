using Cysharp.Threading.Tasks;

public class SimpleTimer
{
    private readonly float _interval;

    public SimpleTimer(float interval, bool startReady = true)
    {
        _interval = interval;
        IsReady = startReady;
    }

    public bool IsReady { get; private set; }

    public async UniTask Wait(float? seconds = null)
    {
        var elapsedSeconds = seconds ?? _interval;

        IsReady = false;
        await UniTask.Delay((int)(elapsedSeconds * 1000));
        IsReady = true;
    }
}