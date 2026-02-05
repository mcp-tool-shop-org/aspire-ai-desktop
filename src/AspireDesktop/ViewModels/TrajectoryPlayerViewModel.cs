using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AspireDesktop.ViewModels;

/// <summary>
/// Shared playback controller for synchronized visualization.
/// All views bind to the same instance.
/// </summary>
public partial class TrajectoryPlayerViewModel : ObservableObject
{
    private System.Timers.Timer? _playbackTimer;
    private const double TickIntervalMs = 16.67; // ~60fps

    private static readonly double[] SpeedValues = [0.25, 0.5, 1.0, 2.0, 4.0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimePercent))]
    [NotifyPropertyChangedFor(nameof(TimeDisplay))]
    private double _time; // 0.0 to 1.0

    [ObservableProperty]
    private double _speed = 1.0; // 0.25x to 4x

    [ObservableProperty]
    private int _speedIndex = 2; // Index into speed array: 0=0.25, 1=0.5, 2=1, 3=2, 4=4

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayPauseIcon))]
    private bool _isPlaying;

    [ObservableProperty]
    private int _totalCycles;

    [ObservableProperty]
    private double _duration = 10.0; // seconds for full playthrough at 1x

    public double TimePercent => Time * 100;

    public string TimeDisplay => $"{Time:P0}";

    public string PlayPauseIcon => IsPlaying ? "pause.png" : "play.png";

    public event Action? TimeChanged;

    public TrajectoryPlayerViewModel()
    {
        _playbackTimer = new System.Timers.Timer(TickIntervalMs);
        _playbackTimer.Elapsed += OnPlaybackTick;
    }

    partial void OnSpeedIndexChanged(int value)
    {
        if (value >= 0 && value < SpeedValues.Length)
        {
            Speed = SpeedValues[value];
        }
    }

    [RelayCommand]
    private void PlayPause()
    {
        IsPlaying = !IsPlaying;

        if (IsPlaying)
        {
            _playbackTimer?.Start();
        }
        else
        {
            _playbackTimer?.Stop();
        }
    }

    [RelayCommand]
    private void Stop()
    {
        IsPlaying = false;
        _playbackTimer?.Stop();
        Time = 0;
        TimeChanged?.Invoke();
    }

    [RelayCommand]
    private void StepForward()
    {
        Time = Math.Min(1.0, Time + 0.01);
        TimeChanged?.Invoke();
    }

    [RelayCommand]
    private void StepBackward()
    {
        Time = Math.Max(0.0, Time - 0.01);
        TimeChanged?.Invoke();
    }

    [RelayCommand]
    private void JumpToTime(double t)
    {
        Time = Math.Clamp(t, 0.0, 1.0);
        TimeChanged?.Invoke();
    }

    public void SetSpeed(double speed)
    {
        Speed = Math.Clamp(speed, 0.25, 4.0);

        // Update SpeedIndex to match
        for (int i = 0; i < SpeedValues.Length; i++)
        {
            if (Math.Abs(SpeedValues[i] - speed) < 0.01)
            {
                SpeedIndex = i;
                break;
            }
        }
    }

    public void IncreaseSpeed()
    {
        if (SpeedIndex < SpeedValues.Length - 1)
            SpeedIndex++;
    }

    public void DecreaseSpeed()
    {
        if (SpeedIndex > 0)
            SpeedIndex--;
    }

    private void OnPlaybackTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        var delta = (TickIntervalMs / 1000.0) / Duration * Speed;
        Time += delta;

        if (Time >= 1.0)
        {
            Time = 1.0;
            IsPlaying = false;
            _playbackTimer?.Stop();
        }

        TimeChanged?.Invoke();
    }

    public void Dispose()
    {
        _playbackTimer?.Stop();
        _playbackTimer?.Dispose();
    }
}
