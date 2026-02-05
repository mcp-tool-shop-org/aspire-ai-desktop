using SkiaSharp;

namespace VortexKit.Core;

/// <summary>
/// Service for exporting visualizations as images and frame sequences.
/// </summary>
public class ExportService
{
    /// <summary>
    /// Default export resolution.
    /// </summary>
    public const int DefaultWidth = 1920;
    public const int DefaultHeight = 1080;

    /// <summary>
    /// Render a canvas to a bitmap at a specific time.
    /// </summary>
    public SKBitmap RenderToBitmap(
        Action<SKCanvas, SKImageInfo, double> renderAction,
        double time,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        var width = options.Width ?? DefaultWidth;
        var height = options.Height ?? DefaultHeight;

        var info = new SKImageInfo(width, height);
        var bitmap = new SKBitmap(info);

        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(options.BackgroundColor ?? VortexColors.Background);

        renderAction(canvas, info, time);

        return bitmap;
    }

    /// <summary>
    /// Export a single frame as PNG.
    /// </summary>
    public async Task<string> ExportFrameAsync(
        Action<SKCanvas, SKImageInfo, double> renderAction,
        double time,
        string outputPath,
        ExportOptions? options = null)
    {
        using var bitmap = RenderToBitmap(renderAction, time, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        await using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        return outputPath;
    }

    /// <summary>
    /// Export a sequence of frames as PNGs.
    /// </summary>
    public async Task<string[]> ExportSequenceAsync(
        Action<SKCanvas, SKImageInfo, double> renderAction,
        string outputDir,
        ExportOptions? options = null)
    {
        options ??= new ExportOptions();

        Directory.CreateDirectory(outputDir);

        var fps = options.Fps ?? 30;
        var duration = options.Duration ?? 5.0;
        var totalFrames = (int)(fps * duration);

        var paths = new List<string>();

        var width = options.Width ?? DefaultWidth;
        var height = options.Height ?? DefaultHeight;
        var info = new SKImageInfo(width, height);

        using var bitmap = new SKBitmap(info);
        using var canvas = new SKCanvas(bitmap);

        for (int frame = 0; frame < totalFrames; frame++)
        {
            var t = (double)frame / (totalFrames - 1);

            canvas.Clear(options.BackgroundColor ?? VortexColors.Background);
            renderAction(canvas, info, t);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            var framePath = Path.Combine(outputDir, $"frame_{frame:D5}.png");
            await using var stream = File.OpenWrite(framePath);
            data.SaveTo(stream);

            paths.Add(framePath);

            options.ProgressCallback?.Invoke(frame + 1, totalFrames);
        }

        // Write info file
        var infoPath = Path.Combine(outputDir, "sequence_info.txt");
        await File.WriteAllTextAsync(infoPath,
            $"VortexKit Frame Sequence Export\n" +
            $"Frames: {totalFrames}\n" +
            $"FPS: {fps}\n" +
            $"Duration: {duration}s\n" +
            $"Resolution: {width}x{height}\n" +
            $"\n" +
            $"To convert to video:\n" +
            $"ffmpeg -framerate {fps} -i frame_%05d.png -c:v libx264 -pix_fmt yuv420p output.mp4\n" +
            $"\n" +
            $"To convert to GIF:\n" +
            $"ffmpeg -framerate {fps} -i frame_%05d.png output.gif\n");

        return paths.ToArray();
    }

    /// <summary>
    /// Export a side-by-side comparison.
    /// </summary>
    public async Task<string> ExportComparisonAsync(
        Action<SKCanvas, SKImageInfo, double> leftRender,
        Action<SKCanvas, SKImageInfo, double> rightRender,
        double time,
        string outputPath,
        ComparisonExportOptions? options = null)
    {
        options ??= new ComparisonExportOptions();

        var width = options.Width ?? DefaultWidth;
        var height = options.Height ?? DefaultHeight;
        var dividerWidth = options.DividerWidth ?? 4;
        var halfWidth = (width - dividerWidth) / 2;

        var info = new SKImageInfo(width, height);
        using var bitmap = new SKBitmap(info);
        using var canvas = new SKCanvas(bitmap);

        // Background
        canvas.Clear(options.BackgroundColor ?? VortexColors.Background);

        // Left panel
        canvas.Save();
        canvas.ClipRect(new SKRect(0, 0, halfWidth, height));
        leftRender(canvas, new SKImageInfo(halfWidth, height), time);
        canvas.Restore();

        // Divider
        using var dividerPaint = new SKPaint
        {
            Color = options.DividerColor ?? VortexColors.Grid,
            StrokeWidth = dividerWidth
        };
        canvas.DrawLine(width / 2f, 0, width / 2f, height, dividerPaint);

        // Right panel
        canvas.Save();
        canvas.Translate(halfWidth + dividerWidth, 0);
        canvas.ClipRect(new SKRect(0, 0, halfWidth, height));
        rightRender(canvas, new SKImageInfo(halfWidth, height), time);
        canvas.Restore();

        // Optional labels
        if (options.ShowLabels)
        {
            DrawComparisonLabels(canvas, width, height, options);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        await using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        return outputPath;
    }

    private void DrawComparisonLabels(SKCanvas canvas, int width, int height, ComparisonExportOptions options)
    {
        using var paint = new SKPaint
        {
            Color = VortexColors.Text,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        var leftLabel = options.LeftLabel ?? "A";
        var rightLabel = options.RightLabel ?? "B";

        // Left label
        paint.Color = options.LeftColor ?? VortexColors.CompareLeft;
        canvas.DrawText(leftLabel, 20, 30, paint);

        // Right label
        paint.Color = options.RightColor ?? VortexColors.CompareRight;
        canvas.DrawText(rightLabel, width / 2 + 20, 30, paint);
    }
}

/// <summary>
/// Options for single-frame or sequence exports.
/// </summary>
public record ExportOptions
{
    public int? Width { get; init; }
    public int? Height { get; init; }
    public int? Fps { get; init; }
    public double? Duration { get; init; }
    public SKColor? BackgroundColor { get; init; }
    public Action<int, int>? ProgressCallback { get; init; }
}

/// <summary>
/// Options for comparison exports.
/// </summary>
public record ComparisonExportOptions : ExportOptions
{
    public int? DividerWidth { get; init; }
    public SKColor? DividerColor { get; init; }
    public bool ShowLabels { get; init; } = true;
    public string? LeftLabel { get; init; }
    public string? RightLabel { get; init; }
    public SKColor? LeftColor { get; init; }
    public SKColor? RightColor { get; init; }
}
