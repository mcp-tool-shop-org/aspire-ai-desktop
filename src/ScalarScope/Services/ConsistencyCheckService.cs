namespace ScalarScope.Services;

/// <summary>
/// Cross-view consistency verification.
/// Ensures the same data means the same thing across all views.
/// </summary>
public static class ConsistencyCheckService
{
    /// <summary>
    /// Event fired when a consistency violation is detected.
    /// </summary>
    public static event Action<ConsistencyViolation>? ViolationDetected;

    /// <summary>
    /// Recent violations for diagnostics.
    /// </summary>
    private static readonly List<ConsistencyViolation> RecentViolations = new();
    private const int MaxStoredViolations = 20;

    // ═══════════════════════════════════════════════════════════════════════
    // TIME MAPPING CONSISTENCY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verify that time t maps to the same index across trajectory, scalars, and eigenvalues.
    /// Returns true if consistent, false if there's a significant mismatch.
    /// </summary>
    public static bool VerifyTimeMapping(
        double t,
        int trajectoryCount,
        int scalarsCount,
        int eigenvaluesCount,
        string context = "unknown")
    {
        if (trajectoryCount == 0 && scalarsCount == 0 && eigenvaluesCount == 0)
            return true; // No data to check

        // Calculate expected indices
        var trajectoryIdx = trajectoryCount > 0 ? (int)(t * Math.Max(0, trajectoryCount - 1)) : -1;
        var scalarsIdx = scalarsCount > 0 ? (int)(t * Math.Max(0, scalarsCount - 1)) : -1;
        var eigenIdx = eigenvaluesCount > 0 ? (int)(t * Math.Max(0, eigenvaluesCount - 1)) : -1;

        // Calculate proportional positions
        var trajectoryPos = trajectoryCount > 0 ? (double)trajectoryIdx / Math.Max(1, trajectoryCount - 1) : -1;
        var scalarsPos = scalarsCount > 0 ? (double)scalarsIdx / Math.Max(1, scalarsCount - 1) : -1;
        var eigenPos = eigenvaluesCount > 0 ? (double)eigenIdx / Math.Max(1, eigenvaluesCount - 1) : -1;

        // Check for significant drift (more than 5% difference in proportional position)
        const double maxDrift = 0.05;
        var positions = new[] { trajectoryPos, scalarsPos, eigenPos }.Where(p => p >= 0).ToList();

        if (positions.Count < 2)
            return true; // Not enough data series to compare

        var minPos = positions.Min();
        var maxPos = positions.Max();
        var drift = maxPos - minPos;

        if (drift > maxDrift)
        {
            ReportViolation(new ConsistencyViolation
            {
                Rule = ConsistencyRule.TimeMappingDrift,
                Message = $"Time {t:F4} maps to different proportional positions: " +
                          $"trajectory={trajectoryPos:F4}, scalars={scalarsPos:F4}, eigen={eigenPos:F4}, drift={drift:F4}",
                Context = context,
                Severity = ConsistencySeverity.Warning,
                Details = new Dictionary<string, object>
                {
                    ["time"] = t,
                    ["trajectoryIdx"] = trajectoryIdx,
                    ["scalarsIdx"] = scalarsIdx,
                    ["eigenIdx"] = eigenIdx,
                    ["drift"] = drift
                }
            });
            return false;
        }

        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EIGENVALUE INTERPRETATION CONSISTENCY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verify that first factor variance calculation is consistent.
    /// This metric is shown in multiple places and must always match.
    /// </summary>
    public static double ComputeFirstFactorVariance(IList<double>? eigenvalues, string context = "unknown")
    {
        if (eigenvalues == null || eigenvalues.Count == 0)
            return 0;

        var total = eigenvalues.Sum();
        if (total < 0.001)
            return 0;

        return eigenvalues[0] / total;
    }

    /// <summary>
    /// Verify that effective dimensionality calculation is consistent.
    /// Uses participation ratio: 1 / sum(p_i^2) where p_i = λ_i / sum(λ)
    /// </summary>
    public static double ComputeEffectiveDimensionality(IList<double>? eigenvalues, string context = "unknown")
    {
        if (eigenvalues == null || eigenvalues.Count == 0)
            return 0;

        var total = eigenvalues.Sum();
        if (total < 0.001)
            return eigenvalues.Count;

        var normalized = eigenvalues.Select(e => e / total).ToList();
        var sumSq = normalized.Sum(n => n * n);

        return sumSq > 0 ? 1.0 / sumSq : eigenvalues.Count;
    }

    /// <summary>
    /// Verify that interpretation threshold is consistent across views.
    /// Returns the interpretation category for a given first factor variance.
    /// </summary>
    public static EigenInterpretation GetEigenInterpretation(double firstFactorVariance)
    {
        // These thresholds must match everywhere they're used:
        // - EigenSpectrumView.DrawInterpretation
        // - ComparisonTrajectoryCanvas.DrawDominanceIndicator
        // - ComparisonTrajectoryCanvas.DrawAnnotations
        // - ComparisonViewModel.GenerateRunDescription

        if (firstFactorVariance > 0.7)
            return EigenInterpretation.StrongSharedAxis;
        else if (firstFactorVariance > 0.5)
            return EigenInterpretation.ModerateUnification;
        else if (firstFactorVariance > 0.3)
            return EigenInterpretation.PartialStructure;
        else
            return EigenInterpretation.OrthogonalEvaluators;
    }

    /// <summary>
    /// Get the display color for an eigenvalue interpretation.
    /// Centralizes color choices for consistency.
    /// </summary>
    public static (byte R, byte G, byte B) GetInterpretationColor(EigenInterpretation interpretation)
    {
        return interpretation switch
        {
            EigenInterpretation.StrongSharedAxis => (144, 238, 144),    // LightGreen
            EigenInterpretation.ModerateUnification => (255, 255, 0),  // Yellow
            EigenInterpretation.PartialStructure => (255, 165, 0),     // Orange
            EigenInterpretation.OrthogonalEvaluators => (255, 99, 71), // Tomato/Red
            _ => (128, 128, 128) // Gray
        };
    }

    // ═══════════════════════════════════════════════════════════════════════
    // COMPARISON VIEW CONSISTENCY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verify that left and right runs in comparison view have compatible data.
    /// Returns warnings if there are significant structural differences.
    /// </summary>
    public static List<string> VerifyComparisonCompatibility(
        int leftTrajectoryCount,
        int rightTrajectoryCount,
        int leftEigenCount,
        int rightEigenCount,
        string context = "unknown")
    {
        var warnings = new List<string>();

        // Check trajectory count ratio
        if (leftTrajectoryCount > 0 && rightTrajectoryCount > 0)
        {
            var ratio = (double)Math.Max(leftTrajectoryCount, rightTrajectoryCount) /
                       Math.Min(leftTrajectoryCount, rightTrajectoryCount);

            if (ratio > 2.0)
            {
                warnings.Add($"Trajectory length mismatch: left={leftTrajectoryCount}, right={rightTrajectoryCount} (ratio={ratio:F1}x)");

                ReportViolation(new ConsistencyViolation
                {
                    Rule = ConsistencyRule.ComparisonDataMismatch,
                    Message = $"Comparison trajectory lengths differ significantly",
                    Context = context,
                    Severity = ConsistencySeverity.Info,
                    Details = new Dictionary<string, object>
                    {
                        ["leftCount"] = leftTrajectoryCount,
                        ["rightCount"] = rightTrajectoryCount,
                        ["ratio"] = ratio
                    }
                });
            }
        }

        // Check eigenvalue count ratio
        if (leftEigenCount > 0 && rightEigenCount > 0)
        {
            var ratio = (double)Math.Max(leftEigenCount, rightEigenCount) /
                       Math.Min(leftEigenCount, rightEigenCount);

            if (ratio > 2.0)
            {
                warnings.Add($"Eigenvalue series length mismatch: left={leftEigenCount}, right={rightEigenCount}");
            }
        }

        return warnings;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // METRIC CALCULATION CONSISTENCY
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Centralized curvature threshold for "high curvature" detection.
    /// Used in TrajectoryCanvas and ComparisonTrajectoryCanvas annotations.
    /// </summary>
    public const double HighCurvatureThreshold = 0.1;

    /// <summary>
    /// Centralized threshold for "dominant" eigenvalue in comparison.
    /// </summary>
    public const double DominantFirstFactorThreshold = 0.6;

    /// <summary>
    /// Centralized threshold for "moderate" eigenvalue in comparison.
    /// </summary>
    public const double ModerateFirstFactorThreshold = 0.35;

    /// <summary>
    /// Centralized threshold for comparing first factor variances.
    /// Delta below this = "too close to call".
    /// </summary>
    public const double FirstFactorDeltaThreshold = 0.1;

    // ═══════════════════════════════════════════════════════════════════════
    // VIOLATION REPORTING
    // ═══════════════════════════════════════════════════════════════════════

    private static void ReportViolation(ConsistencyViolation violation)
    {
        violation.Timestamp = DateTime.UtcNow;

        // Store for diagnostics
        lock (RecentViolations)
        {
            RecentViolations.Add(violation);
            if (RecentViolations.Count > MaxStoredViolations)
                RecentViolations.RemoveAt(0);
        }

        // Log
        System.Diagnostics.Debug.WriteLine(
            $"[Consistency {violation.Severity}] {violation.Rule}: {violation.Message} (context: {violation.Context})");

        // Notify subscribers
        ViolationDetected?.Invoke(violation);
    }

    /// <summary>
    /// Get recent violations for diagnostics.
    /// </summary>
    public static IReadOnlyList<ConsistencyViolation> GetRecentViolations()
    {
        lock (RecentViolations)
        {
            return RecentViolations.ToList();
        }
    }

    /// <summary>
    /// Clear stored violations.
    /// </summary>
    public static void ClearViolations()
    {
        lock (RecentViolations)
        {
            RecentViolations.Clear();
        }
    }
}

/// <summary>
/// Record of a consistency violation.
/// </summary>
public record ConsistencyViolation
{
    public required ConsistencyRule Rule { get; init; }
    public required string Message { get; init; }
    public required string Context { get; init; }
    public required ConsistencySeverity Severity { get; init; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Details { get; init; }
}

/// <summary>
/// Categories of consistency rules.
/// </summary>
public enum ConsistencyRule
{
    TimeMappingDrift,
    EigenInterpretationMismatch,
    ComparisonDataMismatch,
    MetricCalculationDrift
}

/// <summary>
/// Severity of consistency violations.
/// </summary>
public enum ConsistencySeverity
{
    Info,       // Notable but not problematic
    Warning,    // Potential issue, operation continues
    Error       // Significant mismatch, may affect correctness
}

/// <summary>
/// Standardized eigenvalue interpretation categories.
/// </summary>
public enum EigenInterpretation
{
    StrongSharedAxis,       // λ₁ > 70%
    ModerateUnification,    // λ₁ 50-70%
    PartialStructure,       // λ₁ 30-50%
    OrthogonalEvaluators    // λ₁ < 30%
}
