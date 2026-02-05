using System.Text.Json;
using AspireDesktop.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AspireDesktop.ViewModels;

/// <summary>
/// ViewModel for side-by-side run comparison.
/// Manages two runs with synchronized playback.
/// </summary>
public partial class ComparisonViewModel : ObservableObject
{
    // Left run (typically Path A / orthogonal)
    [ObservableProperty]
    private GeometryRun? _leftRun;

    [ObservableProperty]
    private string _leftRunName = "Load Path A";

    [ObservableProperty]
    private bool _hasLeftRun;

    // Right run (typically Path B / correlated)
    [ObservableProperty]
    private GeometryRun? _rightRun;

    [ObservableProperty]
    private string _rightRunName = "Load Path B";

    [ObservableProperty]
    private bool _hasRightRun;

    // Comparison state
    [ObservableProperty]
    private bool _hasBothRuns;

    [ObservableProperty]
    private string _comparisonSummary = "";

    // Shared playback controller
    public TrajectoryPlayerViewModel Player { get; } = new();

    // Computed properties for current time - Left
    public TrajectoryTimestep? LeftCurrentTrajectory => GetTrajectoryAtTime(LeftRun, Player.Time);
    public ScalarTimestep? LeftCurrentScalars => GetScalarsAtTime(LeftRun, Player.Time);
    public EigenTimestep? LeftCurrentEigenvalues => GetEigenvaluesAtTime(LeftRun, Player.Time);

    // Computed properties for current time - Right
    public TrajectoryTimestep? RightCurrentTrajectory => GetTrajectoryAtTime(RightRun, Player.Time);
    public ScalarTimestep? RightCurrentScalars => GetScalarsAtTime(RightRun, Player.Time);
    public EigenTimestep? RightCurrentEigenvalues => GetEigenvaluesAtTime(RightRun, Player.Time);

    public ComparisonViewModel()
    {
        Player.TimeChanged += OnTimeChanged;
    }

    private void OnTimeChanged()
    {
        OnPropertyChanged(nameof(LeftCurrentTrajectory));
        OnPropertyChanged(nameof(LeftCurrentScalars));
        OnPropertyChanged(nameof(LeftCurrentEigenvalues));
        OnPropertyChanged(nameof(RightCurrentTrajectory));
        OnPropertyChanged(nameof(RightCurrentScalars));
        OnPropertyChanged(nameof(RightCurrentEigenvalues));
    }

    [RelayCommand]
    private async Task LoadLeftRunAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select Path A / Orthogonal Run",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".json" } },
                { DevicePlatform.macOS, new[] { "json" } },
            })
        });

        if (result != null)
        {
            await LoadLeftFromFileAsync(result.FullPath);
        }
    }

    [RelayCommand]
    private async Task LoadRightRunAsync()
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select Path B / Correlated Run",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".json" } },
                { DevicePlatform.macOS, new[] { "json" } },
            })
        });

        if (result != null)
        {
            await LoadRightFromFileAsync(result.FullPath);
        }
    }

    public async Task LoadLeftFromFileAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var run = JsonSerializer.Deserialize<GeometryRun>(json);

            if (run != null)
            {
                LeftRun = run;
                LeftRunName = Path.GetFileNameWithoutExtension(path);
                HasLeftRun = true;
                UpdateComparisonState();
            }
        }
        catch (Exception ex)
        {
            LeftRunName = $"Error: {ex.Message}";
            HasLeftRun = false;
        }
    }

    public async Task LoadRightFromFileAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var run = JsonSerializer.Deserialize<GeometryRun>(json);

            if (run != null)
            {
                RightRun = run;
                RightRunName = Path.GetFileNameWithoutExtension(path);
                HasRightRun = true;
                UpdateComparisonState();
            }
        }
        catch (Exception ex)
        {
            RightRunName = $"Error: {ex.Message}";
            HasRightRun = false;
        }
    }

    private void UpdateComparisonState()
    {
        HasBothRuns = HasLeftRun && HasRightRun;

        if (HasBothRuns)
        {
            GenerateComparisonSummary();
            Player.JumpToTimeCommand.Execute(0.0);
        }
    }

    private void GenerateComparisonSummary()
    {
        if (LeftRun == null || RightRun == null) return;

        var leftCondition = LeftRun.Metadata.Condition;
        var rightCondition = RightRun.Metadata.Condition;
        var leftTier = LeftRun.Metadata.ConscienceTier;
        var rightTier = RightRun.Metadata.ConscienceTier;
        var leftFailures = LeftRun.Failures.Count;
        var rightFailures = RightRun.Failures.Count;

        // Compute final eigenvalue comparison
        var leftFinalEigen = LeftRun.Geometry.Eigenvalues.LastOrDefault()?.Values;
        var rightFinalEigen = RightRun.Geometry.Eigenvalues.LastOrDefault()?.Values;

        var leftFirstFactor = leftFinalEigen?.Count > 0
            ? leftFinalEigen[0] / leftFinalEigen.Sum()
            : 0;
        var rightFirstFactor = rightFinalEigen?.Count > 0
            ? rightFinalEigen[0] / rightFinalEigen.Sum()
            : 0;

        ComparisonSummary = $"Left: {leftCondition} ({leftTier}, {leftFailures} failures, λ₁={leftFirstFactor:P0})\n" +
                          $"Right: {rightCondition} ({rightTier}, {rightFailures} failures, λ₁={rightFirstFactor:P0})";
    }

    // Helper methods for getting data at time t
    private static TrajectoryTimestep? GetTrajectoryAtTime(GeometryRun? run, double t)
    {
        if (run?.Trajectory.Timesteps is not { Count: > 0 } steps)
            return null;

        var idx = (int)(t * (steps.Count - 1));
        return steps[Math.Clamp(idx, 0, steps.Count - 1)];
    }

    private static ScalarTimestep? GetScalarsAtTime(GeometryRun? run, double t)
    {
        if (run?.Scalars.Values is not { Count: > 0 } values)
            return null;

        var idx = (int)(t * (values.Count - 1));
        return values[Math.Clamp(idx, 0, values.Count - 1)];
    }

    private static EigenTimestep? GetEigenvaluesAtTime(GeometryRun? run, double t)
    {
        if (run?.Geometry.Eigenvalues is not { Count: > 0 } values)
            return null;

        var idx = (int)(t * (values.Count - 1));
        return values[Math.Clamp(idx, 0, values.Count - 1)];
    }

    public IEnumerable<TrajectoryTimestep> GetLeftTrajectoryUpToTime(double t)
    {
        if (LeftRun?.Trajectory.Timesteps is not { Count: > 0 } steps)
            yield break;

        var maxIdx = (int)(t * (steps.Count - 1));
        for (int i = 0; i <= Math.Min(maxIdx, steps.Count - 1); i++)
        {
            yield return steps[i];
        }
    }

    public IEnumerable<TrajectoryTimestep> GetRightTrajectoryUpToTime(double t)
    {
        if (RightRun?.Trajectory.Timesteps is not { Count: > 0 } steps)
            yield break;

        var maxIdx = (int)(t * (steps.Count - 1));
        for (int i = 0; i <= Math.Min(maxIdx, steps.Count - 1); i++)
        {
            yield return steps[i];
        }
    }

    /// <summary>
    /// Compute comparison metrics at current time.
    /// </summary>
    public ComparisonMetrics GetCurrentMetrics()
    {
        var leftEigen = LeftCurrentEigenvalues?.Values ?? [];
        var rightEigen = RightCurrentEigenvalues?.Values ?? [];

        var leftFirstFactor = leftEigen.Count > 0 ? leftEigen[0] / Math.Max(0.001, leftEigen.Sum()) : 0;
        var rightFirstFactor = rightEigen.Count > 0 ? rightEigen[0] / Math.Max(0.001, rightEigen.Sum()) : 0;

        var leftEffDim = LeftCurrentTrajectory?.EffectiveDim ?? 0;
        var rightEffDim = RightCurrentTrajectory?.EffectiveDim ?? 0;

        var leftCurvature = LeftCurrentTrajectory?.Curvature ?? 0;
        var rightCurvature = RightCurrentTrajectory?.Curvature ?? 0;

        return new ComparisonMetrics
        {
            LeftFirstFactorVariance = leftFirstFactor,
            RightFirstFactorVariance = rightFirstFactor,
            LeftEffectiveDim = leftEffDim,
            RightEffectiveDim = rightEffDim,
            LeftCurvature = leftCurvature,
            RightCurvature = rightCurvature,
            FirstFactorDelta = rightFirstFactor - leftFirstFactor,
            EffectiveDimDelta = leftEffDim - rightEffDim, // Lower is more unified
        };
    }
}

public record ComparisonMetrics
{
    public double LeftFirstFactorVariance { get; init; }
    public double RightFirstFactorVariance { get; init; }
    public double LeftEffectiveDim { get; init; }
    public double RightEffectiveDim { get; init; }
    public double LeftCurvature { get; init; }
    public double RightCurvature { get; init; }
    public double FirstFactorDelta { get; init; }
    public double EffectiveDimDelta { get; init; }
}
