namespace VortexKit.Core;

/// <summary>
/// Interface for time-series data that can be visualized.
/// </summary>
public interface ITimeSeries
{
    /// <summary>
    /// Total number of timesteps.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Get the normalized time (0-1) for a given index.
    /// </summary>
    double GetTimeAtIndex(int index);

    /// <summary>
    /// Get the index for a given normalized time (0-1).
    /// </summary>
    int GetIndexAtTime(double time);
}

/// <summary>
/// Interface for data that can be rendered at a specific time.
/// </summary>
public interface ITimeSeriesData<T> : ITimeSeries
{
    /// <summary>
    /// Get the data at a specific normalized time (0-1).
    /// </summary>
    T? GetAtTime(double time);

    /// <summary>
    /// Get all data up to a specific time (for drawing trails).
    /// </summary>
    IEnumerable<T> GetUpToTime(double time);
}

/// <summary>
/// Base implementation of time series.
/// </summary>
public class TimeSeries<T> : ITimeSeriesData<T>
{
    private readonly IList<T> _data;

    public TimeSeries(IList<T> data)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public int Count => _data.Count;

    public double GetTimeAtIndex(int index)
    {
        if (Count <= 1) return 0;
        return (double)index / (Count - 1);
    }

    public int GetIndexAtTime(double time)
    {
        if (Count == 0) return 0;
        var idx = (int)(time * (Count - 1));
        return Math.Clamp(idx, 0, Count - 1);
    }

    public T? GetAtTime(double time)
    {
        if (Count == 0) return default;
        var idx = GetIndexAtTime(time);
        return _data[idx];
    }

    public IEnumerable<T> GetUpToTime(double time)
    {
        var maxIdx = GetIndexAtTime(time);
        for (int i = 0; i <= maxIdx && i < Count; i++)
        {
            yield return _data[i];
        }
    }

    public T this[int index] => _data[index];
}

/// <summary>
/// Interface for failure/anomaly events.
/// </summary>
public interface IFailureEvent
{
    /// <summary>
    /// Normalized time (0-1) when the failure occurred.
    /// </summary>
    double Time { get; }

    /// <summary>
    /// Category/type of failure.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Severity level (critical, warning, info).
    /// </summary>
    string Severity { get; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Default implementation of failure event.
/// </summary>
public record FailureEvent : IFailureEvent
{
    public double Time { get; init; }
    public string Category { get; init; } = "";
    public string Severity { get; init; } = "info";
    public string Description { get; init; } = "";
}
