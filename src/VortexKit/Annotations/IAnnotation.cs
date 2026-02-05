using SkiaSharp;

namespace VortexKit.Annotations;

/// <summary>
/// Categories of annotations for filtering and styling.
/// </summary>
public enum AnnotationCategory
{
    /// <summary>
    /// Phase transitions and state changes.
    /// </summary>
    Phase,

    /// <summary>
    /// Warnings about curvature, instability, etc.
    /// </summary>
    Warning,

    /// <summary>
    /// Insights about structure (eigenvalues, etc.).
    /// </summary>
    Insight,

    /// <summary>
    /// Failure markers.
    /// </summary>
    Failure,

    /// <summary>
    /// Custom/user-defined annotations.
    /// </summary>
    Custom
}

/// <summary>
/// Interface for visual annotations on time-series data.
/// </summary>
public interface IAnnotation
{
    /// <summary>
    /// Display text for the annotation.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Explanation of what this annotation means theoretically.
    /// </summary>
    string TheoreticalBasis { get; }

    /// <summary>
    /// Category for filtering and styling.
    /// </summary>
    AnnotationCategory Category { get; }

    /// <summary>
    /// Normalized time (0-1) when this annotation applies.
    /// </summary>
    double Time { get; }

    /// <summary>
    /// Position in data coordinates (x, y).
    /// </summary>
    (double X, double Y) Position { get; }

    /// <summary>
    /// Optional importance/priority (higher = more important).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Default implementation of annotation.
/// </summary>
public record Annotation : IAnnotation
{
    public string Label { get; init; } = "";
    public string TheoreticalBasis { get; init; } = "";
    public AnnotationCategory Category { get; init; }
    public double Time { get; init; }
    public (double X, double Y) Position { get; init; }
    public int Priority { get; init; }
}

/// <summary>
/// Extension methods for annotation styling.
/// </summary>
public static class AnnotationExtensions
{
    /// <summary>
    /// Get the default color for an annotation category.
    /// </summary>
    public static SKColor GetCategoryColor(this AnnotationCategory category) => category switch
    {
        AnnotationCategory.Phase => VortexColors.Info,
        AnnotationCategory.Warning => VortexColors.Warning,
        AnnotationCategory.Insight => VortexColors.Success,
        AnnotationCategory.Failure => VortexColors.Danger,
        AnnotationCategory.Custom => VortexColors.Highlight,
        _ => VortexColors.TextMuted
    };

    /// <summary>
    /// Get the default icon for an annotation category.
    /// </summary>
    public static string GetCategoryIcon(this AnnotationCategory category) => category switch
    {
        AnnotationCategory.Phase => "◆",
        AnnotationCategory.Warning => "⚠",
        AnnotationCategory.Insight => "◉",
        AnnotationCategory.Failure => "✕",
        AnnotationCategory.Custom => "●",
        _ => "•"
    };
}
