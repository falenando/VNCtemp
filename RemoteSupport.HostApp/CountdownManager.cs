using System;
using System.Timers;

namespace RemoteSupport.HostApp;

internal sealed class CountdownManager
{
    private readonly Timer _timer;
    private int _remainingSeconds;

    public event Action<int>? RemainingSecondsChanged;
    public event Action? Completed;

    public CountdownManager()
    {
        _timer = new Timer(1000);
        _timer.Elapsed += OnElapsed;
    }

    public void Start(int seconds)
    {
        if (seconds <= 0)
        {
            return;
        }

        _remainingSeconds = seconds;
        RemainingSecondsChanged?.Invoke(_remainingSeconds);
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _remainingSeconds = 0;
        RemainingSecondsChanged?.Invoke(_remainingSeconds);
    }

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            Completed?.Invoke();
            return;
        }

        _remainingSeconds--;
        RemainingSecondsChanged?.Invoke(_remainingSeconds);
        if (_remainingSeconds <= 0)
        {
            _timer.Stop();
            Completed?.Invoke();
        }
    }
}
