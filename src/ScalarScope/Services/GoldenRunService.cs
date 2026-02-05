using System.Text.Json;
using ScalarScope.Models;

namespace ScalarScope.Services;

/// <summary>
/// Service for managing "golden" reference runs.
/// Golden runs are known-good outputs that can be compared against
/// to detect accidental regressions in calculations or rendering.
/// </summary>
public static class GoldenRunService
{
    private static readonly string GoldenRunsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ScalarScope",
        "GoldenRuns");

    /// <summary>
    /// Snapshot of computed values at a specific time point.
    /// Used for regression testing.
    /// </summary>
    public record GoldenSnapshot
    {
        public required string RunId { get; init; }
        public required string RunName { get; init; }
        public required double Time { get; init; }
        public DateTime CapturedAt { get; init; }
        public string Version { get; init; } = "1.0.0";

        // Trajectory values
        public double EffectiveDim { get; init; }
        public double Curvature { get; init; }
        public List<double> State2D { get; init; } = [];

        // Eigenvalue values
        public List<double> Eigenvalues { get; init; } = [];
        public double FirstFactorVariance { get; init; }
        public double ComputedEffectiveDim { get; init; }
        public EigenInterpretation Interpretation { get; init; }

        // Scalar values (if available)
        public double Correctness { get; init; }
        public double Coherence { get; init; }
        public double Calibration { get; init; }

        // Hash for quick comparison
        public string ContentHash { get; init; } = "";
    }

    /// <summary>
    /// Result of comparing a current run against a golden snapshot.
    /// </summary>
    public record ComparisonResult
    {
        public required string RunId { get; init; }
        public required double Time { get; init; }
        public bool IsMatch { get; init; }
        public List<string> Differences { get; init; } = [];
        public double DriftScore { get; init; } // 0 = identical, higher = more drift
    }

    /// <summary>
    /// Capture a golden snapshot from the current run state.
    /// </summary>
    public static GoldenSnapshot CaptureSnapshot(
        GeometryRun run,
        double time,
        string runId,
        string runName)
    {
        var trajectoryCount = run.Trajectory?.Timesteps?.Count ?? 0;
        var eigenCount = run.Geometry?.Eigenvalues?.Count ?? 0;
        var scalarsCount = run.Scalars?.Values?.Count ?? 0;

        // Get trajectory state
        var trajectoryIdx = trajectoryCount > 0 ? (int)(time * Math.Max(0, trajectoryCount - 1)) : 0;
        trajectoryIdx = Math.Clamp(trajectoryIdx, 0, Math.Max(0, trajectoryCount - 1));
        var trajectory = trajectoryCount > 0 ? run.Trajectory!.Timesteps![trajectoryIdx] : null;

        // Get eigenvalue state
        var eigenIdx = eigenCount > 0 ? (int)(time * Math.Max(0, eigenCount - 1)) : 0;
        eigenIdx = Math.Clamp(eigenIdx, 0, Math.Max(0, eigenCount - 1));
        var eigen = eigenCount > 0 ? run.Geometry!.Eigenvalues![eigenIdx] : null;

        // Get scalar state
        var scalarsIdx = scalarsCount > 0 ? (int)(time * Math.Max(0, scalarsCount - 1)) : 0;
        scalarsIdx = Math.Clamp(scalarsIdx, 0, Math.Max(0, scalarsCount - 1));
        var scalars = scalarsCount > 0 ? run.Scalars!.Values![scalarsIdx] : null;

        // Compute derived values using centralized calculations
        var eigenValues = eigen?.Values ?? [];
        var firstFactorVariance = ConsistencyCheckService.ComputeFirstFactorVariance(eigenValues, "GoldenSnapshot");
        var computedEffDim = ConsistencyCheckService.ComputeEffectiveDimensionality(eigenValues, "GoldenSnapshot");
        var interpretation = ConsistencyCheckService.GetEigenInterpretation(firstFactorVariance);

        var snapshot = new GoldenSnapshot
        {
            RunId = runId,
            RunName = runName,
            Time = time,
            CapturedAt = DateTime.UtcNow,
            EffectiveDim = trajectory?.EffectiveDim ?? 0,
            Curvature = trajectory?.Curvature ?? 0,
            State2D = trajectory?.State2D?.ToList() ?? [],
            Eigenvalues = eigenValues?.ToList() ?? [],
            FirstFactorVariance = firstFactorVariance,
            ComputedEffectiveDim = computedEffDim,
            Interpretation = interpretation,
            Correctness = scalars?.Correctness ?? 0,
            Coherence = scalars?.Coherence ?? 0,
            Calibration = scalars?.Calibration ?? 0
        };

        // Compute content hash for quick comparison
        var hashContent = $"{snapshot.EffectiveDim:F6}|{snapshot.Curvature:F6}|{snapshot.FirstFactorVariance:F6}|{snapshot.Interpretation}";
        snapshot = snapshot with { ContentHash = ComputeSimpleHash(hashContent) };

        return snapshot;
    }

    /// <summary>
    /// Compare current run state against a golden snapshot.
    /// </summary>
    public static ComparisonResult CompareAgainstGolden(
        GeometryRun run,
        GoldenSnapshot golden)
    {
        var current = CaptureSnapshot(run, golden.Time, golden.RunId, golden.RunName);
        var differences = new List<string>();
        var driftScore = 0.0;

        // Compare effective dimensionality
        var effDimDiff = Math.Abs(current.EffectiveDim - golden.EffectiveDim);
        if (effDimDiff > 0.01)
        {
            differences.Add($"EffectiveDim: {golden.EffectiveDim:F4} -> {current.EffectiveDim:F4} (diff: {effDimDiff:F4})");
            driftScore += effDimDiff;
        }

        // Compare curvature
        var curvatureDiff = Math.Abs(current.Curvature - golden.Curvature);
        if (curvatureDiff > 0.001)
        {
            differences.Add($"Curvature: {golden.Curvature:F4} -> {current.Curvature:F4} (diff: {curvatureDiff:F4})");
            driftScore += curvatureDiff * 10; // Weight curvature more heavily
        }

        // Compare first factor variance
        var ffvDiff = Math.Abs(current.FirstFactorVariance - golden.FirstFactorVariance);
        if (ffvDiff > 0.001)
        {
            differences.Add($"FirstFactorVariance: {golden.FirstFactorVariance:F4} -> {current.FirstFactorVariance:F4} (diff: {ffvDiff:F4})");
            driftScore += ffvDiff * 5;
        }

        // Compare interpretation category
        if (current.Interpretation != golden.Interpretation)
        {
            differences.Add($"Interpretation: {golden.Interpretation} -> {current.Interpretation}");
            driftScore += 1.0; // Category change is significant
        }

        // Compare eigenvalues if available
        if (current.Eigenvalues.Count > 0 && golden.Eigenvalues.Count > 0)
        {
            var minCount = Math.Min(current.Eigenvalues.Count, golden.Eigenvalues.Count);
            for (int i = 0; i < Math.Min(5, minCount); i++) // Check top 5 eigenvalues
            {
                var eigenDiff = Math.Abs(current.Eigenvalues[i] - golden.Eigenvalues[i]);
                if (eigenDiff > 0.01)
                {
                    differences.Add($"Eigenvalue[{i}]: {golden.Eigenvalues[i]:F4} -> {current.Eigenvalues[i]:F4}");
                    driftScore += eigenDiff * 0.5;
                }
            }
        }

        return new ComparisonResult
        {
            RunId = golden.RunId,
            Time = golden.Time,
            IsMatch = differences.Count == 0,
            Differences = differences,
            DriftScore = driftScore
        };
    }

    /// <summary>
    /// Save a golden snapshot to disk.
    /// </summary>
    public static async Task SaveGoldenSnapshotAsync(GoldenSnapshot snapshot)
    {
        Directory.CreateDirectory(GoldenRunsDirectory);

        var filename = $"{snapshot.RunId}_t{snapshot.Time:F2}.golden.json";
        var path = Path.Combine(GoldenRunsDirectory, filename);

        var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    /// <summary>
    /// Load a golden snapshot from disk.
    /// </summary>
    public static async Task<GoldenSnapshot?> LoadGoldenSnapshotAsync(string runId, double time)
    {
        var filename = $"{runId}_t{time:F2}.golden.json";
        var path = Path.Combine(GoldenRunsDirectory, filename);

        if (!File.Exists(path))
            return null;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<GoldenSnapshot>(json);
    }

    /// <summary>
    /// List all available golden snapshots.
    /// </summary>
    public static IEnumerable<(string RunId, double Time, DateTime CapturedAt)> ListGoldenSnapshots()
    {
        if (!Directory.Exists(GoldenRunsDirectory))
            yield break;

        foreach (var file in Directory.GetFiles(GoldenRunsDirectory, "*.golden.json"))
        {
            // Parse filename: {runId}_t{time}.golden.json
            var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
            var parts = name.Split("_t");
            if (parts.Length == 2 && double.TryParse(parts[1], out var time))
            {
                var info = new FileInfo(file);
                yield return (parts[0], time, info.CreationTimeUtc);
            }
        }
    }

    /// <summary>
    /// Run golden snapshot validation for all saved snapshots.
    /// Returns a summary of results.
    /// </summary>
    public static async Task<GoldenValidationReport> ValidateAgainstAllGoldenAsync(GeometryRun run, string runId)
    {
        var report = new GoldenValidationReport
        {
            RunId = runId,
            ValidatedAt = DateTime.UtcNow
        };

        var snapshots = ListGoldenSnapshots().Where(s => s.RunId == runId).ToList();

        foreach (var (_, time, _) in snapshots)
        {
            var golden = await LoadGoldenSnapshotAsync(runId, time);
            if (golden != null)
            {
                var result = CompareAgainstGolden(run, golden);
                report.Results.Add(result);

                if (!result.IsMatch)
                    report.TotalDriftScore += result.DriftScore;
            }
        }

        report.AllPassed = report.Results.All(r => r.IsMatch);

        return report;
    }

    /// <summary>
    /// Capture golden snapshots at standard time points (0, 0.25, 0.5, 0.75, 1.0).
    /// </summary>
    public static async Task CaptureStandardGoldenSetAsync(GeometryRun run, string runId, string runName)
    {
        var standardTimes = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };

        foreach (var time in standardTimes)
        {
            var snapshot = CaptureSnapshot(run, time, runId, runName);
            await SaveGoldenSnapshotAsync(snapshot);
        }
    }

    private static string ComputeSimpleHash(string content)
    {
        // Simple hash for quick comparison - not cryptographic
        var hash = 0;
        foreach (var c in content)
        {
            hash = (hash * 31 + c) & 0x7FFFFFFF;
        }
        return hash.ToString("X8");
    }
}

/// <summary>
/// Report from validating a run against golden snapshots.
/// </summary>
public record GoldenValidationReport
{
    public required string RunId { get; init; }
    public DateTime ValidatedAt { get; init; }
    public List<GoldenRunService.ComparisonResult> Results { get; init; } = [];
    public bool AllPassed { get; set; }
    public double TotalDriftScore { get; set; }

    public string ToSummary()
    {
        if (Results.Count == 0)
            return "No golden snapshots found for this run.";

        var passed = Results.Count(r => r.IsMatch);
        var failed = Results.Count - passed;

        var summary = $"Golden validation: {passed}/{Results.Count} passed";
        if (failed > 0)
        {
            summary += $", drift score: {TotalDriftScore:F2}";
            var firstFail = Results.FirstOrDefault(r => !r.IsMatch);
            if (firstFail != null)
            {
                summary += $"\nFirst failure at t={firstFail.Time:F2}: {string.Join(", ", firstFail.Differences.Take(2))}";
            }
        }

        return summary;
    }
}
