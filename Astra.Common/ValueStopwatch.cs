using System.Diagnostics;

namespace Astra.Common;

public struct ValueStopwatch
{
    private long _startingTicks;

    public void Restart()
    {
        _startingTicks = Stopwatch.GetTimestamp();
    }

    public double ElapsedMilliseconds => Elapsed.TotalMilliseconds;
    
    public TimeSpan Elapsed
    {
        get
        {
            var stamp = Stopwatch.GetTimestamp();
            return TimeSpan.FromTicks(stamp - _startingTicks);
        }
    }

    public static ValueStopwatch Create()
    {
        var stopwatch = new ValueStopwatch();
        stopwatch.Restart();
        return stopwatch;
    }
}

public readonly struct StopwatchLogger(ValueStopwatch stopwatch, string? label = null) : IDisposable
{
    public void Dispose()
    {
        var elapsed = stopwatch.Elapsed.Microseconds;
        Console.WriteLine($"{label ?? "Elapsed: "}{elapsed} us");
    }

    public static StopwatchLogger Create(string? label = null) => new(ValueStopwatch.Create(), label);
}
