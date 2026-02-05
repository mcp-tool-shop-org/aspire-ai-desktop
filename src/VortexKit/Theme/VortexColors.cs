using SkiaSharp;

namespace VortexKit;

/// <summary>
/// Semantic color palette for VortexKit visualizations.
/// Designed for dark backgrounds with high contrast and accessibility.
/// </summary>
public static class VortexColors
{
    #region Background Layers

    /// <summary>
    /// Deepest background color.
    /// </summary>
    public static SKColor Background => SKColor.Parse("#0f0f1a");

    /// <summary>
    /// Surface color for cards and panels.
    /// </summary>
    public static SKColor Surface => SKColor.Parse("#1a1a2e");

    /// <summary>
    /// Elevated surface color.
    /// </summary>
    public static SKColor Elevated => SKColor.Parse("#16213e");

    /// <summary>
    /// Grid and divider color.
    /// </summary>
    public static SKColor Grid => SKColor.Parse("#2a2a4e");

    #endregion

    #region Semantic Accents

    /// <summary>
    /// Primary accent color (cyan).
    /// </summary>
    public static SKColor Primary => SKColor.Parse("#00d9ff");

    /// <summary>
    /// Success/positive color (teal). Also used for "left" in comparisons.
    /// </summary>
    public static SKColor Success => SKColor.Parse("#4ecdc4");

    /// <summary>
    /// Danger/negative color (coral). Also used for "right" in comparisons.
    /// </summary>
    public static SKColor Danger => SKColor.Parse("#ff6b6b");

    /// <summary>
    /// Warning color (orange).
    /// </summary>
    public static SKColor Warning => SKColor.Parse("#ff9f43");

    /// <summary>
    /// Info/neutral color (purple).
    /// </summary>
    public static SKColor Info => SKColor.Parse("#a29bfe");

    /// <summary>
    /// Highlight/emphasis color (yellow).
    /// </summary>
    public static SKColor Highlight => SKColor.Parse("#ffd93d");

    /// <summary>
    /// Holdout/special marker color (pink).
    /// </summary>
    public static SKColor Holdout => SKColor.Parse("#fd79a8");

    #endregion

    #region Text Colors

    /// <summary>
    /// Primary text color.
    /// </summary>
    public static SKColor Text => SKColors.White;

    /// <summary>
    /// Secondary/muted text color.
    /// </summary>
    public static SKColor TextMuted => SKColors.White.WithAlpha(180);

    /// <summary>
    /// Disabled/hint text color.
    /// </summary>
    public static SKColor TextDisabled => SKColors.White.WithAlpha(100);

    #endregion

    #region Comparison Colors

    /// <summary>
    /// Left side of comparison (typically Path A / control).
    /// </summary>
    public static SKColor CompareLeft => Success;

    /// <summary>
    /// Right side of comparison (typically Path B / treatment).
    /// </summary>
    public static SKColor CompareRight => Danger;

    #endregion

    #region Severity Colors

    /// <summary>
    /// Critical severity (failures, errors).
    /// </summary>
    public static SKColor SeverityCritical => Danger;

    /// <summary>
    /// Warning severity.
    /// </summary>
    public static SKColor SeverityWarning => Warning;

    /// <summary>
    /// Info severity.
    /// </summary>
    public static SKColor SeverityInfo => Highlight;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get severity color by name.
    /// </summary>
    public static SKColor GetSeverityColor(string severity) => severity.ToLowerInvariant() switch
    {
        "critical" => SeverityCritical,
        "warning" => SeverityWarning,
        "info" => SeverityInfo,
        _ => TextMuted
    };

    /// <summary>
    /// Get color for eigenvalue bar by index.
    /// </summary>
    public static SKColor GetEigenColor(int index) => index switch
    {
        0 => Success,
        1 => Warning,
        2 => Info,
        3 => Holdout,
        4 => Highlight,
        _ => TextMuted
    };

    /// <summary>
    /// Interpolate between two colors.
    /// </summary>
    public static SKColor Lerp(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new SKColor(
            (byte)(a.Red + (b.Red - a.Red) * t),
            (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t),
            (byte)(a.Alpha + (b.Alpha - a.Alpha) * t)
        );
    }

    /// <summary>
    /// Create gradient from one color to another.
    /// </summary>
    public static SKColor[] CreateGradient(SKColor from, SKColor to, int steps)
    {
        var colors = new SKColor[steps];
        for (int i = 0; i < steps; i++)
        {
            colors[i] = Lerp(from, to, (float)i / (steps - 1));
        }
        return colors;
    }

    #endregion
}
