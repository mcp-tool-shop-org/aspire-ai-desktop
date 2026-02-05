using AspireDesktop.Models;
using AspireDesktop.ViewModels;
using SkiaSharp;

namespace AspireDesktop.Services;

/// <summary>
/// Service for exporting visualizations as images and video clips.
/// </summary>
public class ExportService
{
    private const int DefaultWidth = 1920;
    private const int DefaultHeight = 1080;

    /// <summary>
    /// Export current trajectory view as a PNG image.
    /// </summary>
    public async Task<string> ExportStillAsync(
        GeometryRun run,
        double time,
        string outputPath,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        var width = options.Width ?? DefaultWidth;
        var height = options.Height ?? DefaultHeight;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Render the visualization
        RenderTrajectoryFrame(canvas, run, time, width, height, options);

        // Save to file
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        await using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        return outputPath;
    }

    /// <summary>
    /// Export trajectory animation as a sequence of frames.
    /// Can be combined into GIF or video externally.
    /// </summary>
    public async Task<string[]> ExportFrameSequenceAsync(
        GeometryRun run,
        string outputDir,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        Directory.CreateDirectory(outputDir);

        var width = options.Width ?? DefaultWidth;
        var height = options.Height ?? DefaultHeight;
        var fps = options.Fps ?? 30;
        var duration = options.Duration ?? 5.0; // seconds
        var totalFrames = (int)(fps * duration);

        var paths = new List<string>();

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        for (int frame = 0; frame < totalFrames; frame++)
        {
            var t = (double)frame / (totalFrames - 1);

            canvas.Clear();
            RenderTrajectoryFrame(canvas, run, t, width, height, options);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var framePath = Path.Combine(outputDir, $"frame_{frame:D5}.png");
            await using var stream = File.OpenWrite(framePath);
            data.SaveTo(stream);

            paths.Add(framePath);
        }

        return paths.ToArray();
    }

    /// <summary>
    /// Export comparison view as a side-by-side image.
    /// </summary>
    public async Task<string> ExportComparisonStillAsync(
        GeometryRun leftRun,
        GeometryRun rightRun,
        double time,
        string outputPath,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        var width = options.Width ?? DefaultWidth;
        var height = options.Height ?? DefaultHeight;
        var halfWidth = width / 2 - 2;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;

        // Background
        canvas.Clear(SKColor.Parse("#0f0f1a"));

        // Left panel
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, halfWidth, height));
        RenderTrajectoryFrame(canvas, leftRun, time, halfWidth, height, options with
        {
            Label = "Path A (Orthogonal)",
            AccentColor = SKColor.Parse("#4ecdc4")
        });
        canvas.Restore();

        // Divider
        using var dividerPaint = new SKPaint { Color = SKColor.Parse("#2a2a4e"), StrokeWidth = 4 };
        canvas.DrawLine(width / 2f, 0, width / 2f, height, dividerPaint);

        // Right panel
        canvas.Save();
        canvas.Translate(width / 2f + 2, 0);
        canvas.ClipRect(new SKRect(0, 0, halfWidth, height));
        RenderTrajectoryFrame(canvas, rightRun, time, halfWidth, height, options with
        {
            Label = "Path B (Correlated)",
            AccentColor = SKColor.Parse("#ff6b6b")
        });
        canvas.Restore();

        // Title bar
        DrawTitleBar(canvas, leftRun, rightRun, time, width);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        await using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        return outputPath;
    }

    private void RenderTrajectoryFrame(
        SKCanvas canvas,
        GeometryRun run,
        double time,
        int width,
        int height,
        ExportOptions options)
    {
        var backgroundColor = SKColor.Parse("#0f0f1a");
        var gridColor = SKColor.Parse("#2a2a4e");
        var accentColor = options.AccentColor ?? SKColor.Parse("#00d9ff");

        canvas.Clear(backgroundColor);

        var center = new SKPoint(width / 2f, height / 2f);
        var scale = Math.Min(width, height) / 4f;

        // Draw grid
        DrawGrid(canvas, width, height, center, scale, gridColor);

        // Draw label if provided
        if (!string.IsNullOrEmpty(options.Label))
        {
            using var labelPaint = new SKPaint
            {
                Color = accentColor,
                TextSize = 24,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            canvas.DrawText(options.Label, 20, 40, labelPaint);
        }

        // Draw professor vectors
        if (options.ShowProfessors)
        {
            DrawProfessorVectors(canvas, run, center, scale);
        }

        // Draw trajectory
        DrawTrajectory(canvas, run, time, center, scale, accentColor);

        // Draw current position
        DrawCurrentPosition(canvas, run, time, center, scale, accentColor);

        // Draw metrics
        if (options.ShowMetrics)
        {
            DrawMetrics(canvas, run, time, width, height);
        }

        // Draw eigenvalue spectrum
        if (options.ShowEigenvalues)
        {
            DrawEigenSpectrum(canvas, run, time, width, height);
        }
    }

    private void DrawGrid(SKCanvas canvas, int width, int height, SKPoint center, float scale, SKColor gridColor)
    {
        using var paint = new SKPaint
        {
            Color = gridColor,
            StrokeWidth = 1,
            IsAntialias = true
        };

        canvas.DrawLine(0, center.Y, width, center.Y, paint);
        canvas.DrawLine(center.X, 0, center.X, height, paint);

        paint.PathEffect = SKPathEffect.CreateDash([5, 5], 0);
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;
            var offset = i * scale / 2;
            canvas.DrawLine(0, center.Y + offset, width, center.Y + offset, paint);
            canvas.DrawLine(center.X + offset, 0, center.X + offset, height, paint);
        }
    }

    private void DrawProfessorVectors(SKCanvas canvas, GeometryRun run, SKPoint center, float scale)
    {
        var professors = run.Evaluators.Professors;
        if (professors.Count == 0) return;

        using var paint = new SKPaint
        {
            StrokeWidth = 3,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            Color = SKColor.Parse("#a29bfe").WithAlpha(180)
        };

        using var holdoutPaint = new SKPaint
        {
            StrokeWidth = 3,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            Color = SKColor.Parse("#fd79a8").WithAlpha(180)
        };

        foreach (var prof in professors)
        {
            if (prof.Vector.Count < 2) continue;
            var end = ToScreen(prof.Vector, center, scale);
            DrawArrow(canvas, center, end, prof.Holdout ? holdoutPaint : paint);
        }
    }

    private void DrawTrajectory(SKCanvas canvas, GeometryRun run, double time, SKPoint center, float scale, SKColor accentColor)
    {
        var steps = run.Trajectory.Timesteps;
        if (steps.Count < 2) return;

        var maxIdx = (int)(time * (steps.Count - 1));

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round
        };

        for (int i = 1; i <= maxIdx && i < steps.Count; i++)
        {
            var t = (float)i / steps.Count;
            paint.Color = accentColor.WithAlpha((byte)(100 + t * 155));

            var p1 = ToScreen(steps[i - 1].State2D, center, scale);
            var p2 = ToScreen(steps[i].State2D, center, scale);
            canvas.DrawLine(p1, p2, paint);
        }
    }

    private void DrawCurrentPosition(SKCanvas canvas, GeometryRun run, double time, SKPoint center, float scale, SKColor accentColor)
    {
        var steps = run.Trajectory.Timesteps;
        if (steps.Count == 0) return;

        var idx = (int)(time * (steps.Count - 1));
        idx = Math.Clamp(idx, 0, steps.Count - 1);
        var current = steps[idx];

        if (current.State2D.Count < 2) return;

        var pos = ToScreen(current.State2D, center, scale);

        using var glowPaint = new SKPaint
        {
            Color = accentColor.WithAlpha(120),
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 12)
        };
        canvas.DrawCircle(pos, 18, glowPaint);

        using var paint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        canvas.DrawCircle(pos, 8, paint);
    }

    private void DrawMetrics(SKCanvas canvas, GeometryRun run, double time, int width, int height)
    {
        var steps = run.Trajectory.Timesteps;
        if (steps.Count == 0) return;

        var idx = (int)(time * (steps.Count - 1));
        idx = Math.Clamp(idx, 0, steps.Count - 1);
        var current = steps[idx];

        using var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(200),
            TextSize = 16,
            IsAntialias = true
        };

        var x = 20f;
        var y = height - 60f;

        canvas.DrawText($"Time: {time:P0}", x, y, paint);
        y += 22;
        canvas.DrawText($"Eff.Dim: {current.EffectiveDim:F2}", x, y, paint);
        y += 22;
        canvas.DrawText($"Curvature: {current.Curvature:F3}", x, y, paint);
    }

    private void DrawEigenSpectrum(SKCanvas canvas, GeometryRun run, double time, int width, int height)
    {
        var eigenvalues = run.Geometry.Eigenvalues;
        if (eigenvalues.Count == 0) return;

        var eigenIdx = (int)(time * (eigenvalues.Count - 1));
        eigenIdx = Math.Clamp(eigenIdx, 0, eigenvalues.Count - 1);
        var eigen = eigenvalues[eigenIdx];

        if (eigen.Values.Count == 0) return;

        var total = eigen.Values.Sum();
        if (total <= 0) return;

        // Draw small bar chart in corner
        var chartX = width - 200f;
        var chartY = height - 120f;
        var barWidth = 30f;
        var maxBarHeight = 80f;

        using var barPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        using var textPaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(180),
            TextSize = 12,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        var colors = new[]
        {
            SKColor.Parse("#4ecdc4"),
            SKColor.Parse("#ff9f43"),
            SKColor.Parse("#a29bfe"),
            SKColor.Parse("#fd79a8"),
            SKColor.Parse("#ffd93d")
        };

        for (int i = 0; i < Math.Min(eigen.Values.Count, 5); i++)
        {
            var fraction = eigen.Values[i] / total;
            var barHeight = (float)(fraction * maxBarHeight);
            var x = chartX + i * (barWidth + 5);

            barPaint.Color = colors[i % colors.Length];
            canvas.DrawRect(x, chartY + maxBarHeight - barHeight, barWidth, barHeight, barPaint);
            canvas.DrawText($"λ{i + 1}", x + barWidth / 2, chartY + maxBarHeight + 15, textPaint);
        }
    }

    private void DrawTitleBar(SKCanvas canvas, GeometryRun leftRun, GeometryRun rightRun, double time, int width)
    {
        using var bgPaint = new SKPaint
        {
            Color = SKColor.Parse("#1a1a2e"),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(0, 0, width, 60, bgPaint);

        using var titlePaint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawText("ASPIRE Conscience Formation Comparison", width / 2f, 35, titlePaint);

        using var subtitlePaint = new SKPaint
        {
            Color = SKColors.White.WithAlpha(150),
            TextSize = 12,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        canvas.DrawText($"Time: {time:P0} | Left: {leftRun.Metadata.Condition} | Right: {rightRun.Metadata.Condition}",
            width / 2f, 52, subtitlePaint);
    }

    private void DrawArrow(SKCanvas canvas, SKPoint from, SKPoint to, SKPaint paint)
    {
        canvas.DrawLine(from, to, paint);

        var angle = MathF.Atan2(to.Y - from.Y, to.X - from.X);
        var headLen = 10f;
        var headAngle = 0.5f;

        var p1 = new SKPoint(
            to.X - headLen * MathF.Cos(angle - headAngle),
            to.Y - headLen * MathF.Sin(angle - headAngle));
        var p2 = new SKPoint(
            to.X - headLen * MathF.Cos(angle + headAngle),
            to.Y - headLen * MathF.Sin(angle + headAngle));

        canvas.DrawLine(to, p1, paint);
        canvas.DrawLine(to, p2, paint);
    }

    private static SKPoint ToScreen(IList<double> state, SKPoint center, float scale)
    {
        if (state.Count < 2) return center;
        return new SKPoint(
            center.X + (float)state[0] * scale,
            center.Y - (float)state[1] * scale);
    }
}

public record ExportOptions
{
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Fps { get; init; }
    public double? Duration { get; init; }
    public string? Label { get; init; }
    public SKColor? AccentColor { get; init; }
    public bool ShowProfessors { get; init; } = true;
    public bool ShowMetrics { get; init; } = true;
    public bool ShowEigenvalues { get; init; } = true;
    public bool ShowAnnotations { get; init; } = false;
}
