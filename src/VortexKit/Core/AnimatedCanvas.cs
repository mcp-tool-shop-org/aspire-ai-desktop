using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace VortexKit.Core;

/// <summary>
/// Base class for time-synchronized SkiaSharp canvases.
/// Provides automatic invalidation on time change and standard interaction patterns.
/// </summary>
public abstract class AnimatedCanvas : SKCanvasView
{
    public static readonly BindableProperty CurrentTimeProperty =
        BindableProperty.Create(
            nameof(CurrentTime),
            typeof(double),
            typeof(AnimatedCanvas),
            0.0,
            propertyChanged: OnTimeChanged);

    public static readonly BindableProperty ShowGridProperty =
        BindableProperty.Create(
            nameof(ShowGrid),
            typeof(bool),
            typeof(AnimatedCanvas),
            true,
            propertyChanged: OnPropertyChangedInvalidate);

    public static readonly BindableProperty AnimationEnabledProperty =
        BindableProperty.Create(
            nameof(AnimationEnabled),
            typeof(bool),
            typeof(AnimatedCanvas),
            true);

    /// <summary>
    /// Current time position (0.0 to 1.0). Changes trigger re-render.
    /// </summary>
    public double CurrentTime
    {
        get => (double)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    /// <summary>
    /// Whether to show background grid lines.
    /// </summary>
    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// Whether smooth animations are enabled.
    /// </summary>
    public bool AnimationEnabled
    {
        get => (bool)GetValue(AnimationEnabledProperty);
        set => SetValue(AnimationEnabledProperty, value);
    }

    /// <summary>
    /// Fired when user taps on the canvas.
    /// </summary>
    public event Action<SKPoint>? Tapped;

    /// <summary>
    /// Fired when user drags on the canvas.
    /// </summary>
    public event Action<SKPoint, SKPoint>? Dragged;

    protected SKPoint? LastTouchPoint { get; private set; }

    protected AnimatedCanvas()
    {
        PaintSurface += OnPaintSurface;
        EnableTouchEvents = true;
        Touch += OnTouch;
    }

    private static void OnTimeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedCanvas canvas)
        {
            canvas.OnTimeUpdated((double)oldValue, (double)newValue);
            canvas.InvalidateSurface();
        }
    }

    protected static void OnPropertyChangedInvalidate(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AnimatedCanvas canvas)
            canvas.InvalidateSurface();
    }

    /// <summary>
    /// Called when time changes. Override for custom time-change handling.
    /// </summary>
    protected virtual void OnTimeUpdated(double oldTime, double newTime) { }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;

        // Clear with background
        canvas.Clear(GetBackgroundColor());

        // Optional grid
        if (ShowGrid)
            DrawGrid(canvas, info);

        // Subclass rendering
        OnRender(canvas, info, CurrentTime);

        // Optional overlay (for annotations, etc.)
        OnRenderOverlay(canvas, info, CurrentTime);
    }

    /// <summary>
    /// Main rendering method. Override to implement custom visualization.
    /// </summary>
    protected abstract void OnRender(SKCanvas canvas, SKImageInfo info, double time);

    /// <summary>
    /// Optional overlay rendering (annotations, labels, etc.).
    /// </summary>
    protected virtual void OnRenderOverlay(SKCanvas canvas, SKImageInfo info, double time) { }

    /// <summary>
    /// Background color for the canvas.
    /// </summary>
    protected virtual SKColor GetBackgroundColor() => VortexColors.Background;

    /// <summary>
    /// Draw background grid.
    /// </summary>
    protected virtual void DrawGrid(SKCanvas canvas, SKImageInfo info)
    {
        var center = new SKPoint(info.Width / 2f, info.Height / 2f);

        using var paint = new SKPaint
        {
            Color = VortexColors.Grid,
            StrokeWidth = 1,
            IsAntialias = true
        };

        // Center axes
        canvas.DrawLine(0, center.Y, info.Width, center.Y, paint);
        canvas.DrawLine(center.X, 0, center.X, info.Height, paint);

        // Grid lines
        paint.PathEffect = SKPathEffect.CreateDash([5, 5], 0);
        var gridSpacing = Math.Min(info.Width, info.Height) / 8f;

        for (int i = 1; i <= 4; i++)
        {
            var offset = i * gridSpacing;

            // Horizontal
            canvas.DrawLine(0, center.Y + offset, info.Width, center.Y + offset, paint);
            canvas.DrawLine(0, center.Y - offset, info.Width, center.Y - offset, paint);

            // Vertical
            canvas.DrawLine(center.X + offset, 0, center.X + offset, info.Height, paint);
            canvas.DrawLine(center.X - offset, 0, center.X - offset, info.Height, paint);
        }
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                LastTouchPoint = e.Location;
                e.Handled = true;
                break;

            case SKTouchAction.Moved:
                if (LastTouchPoint.HasValue)
                {
                    Dragged?.Invoke(LastTouchPoint.Value, e.Location);
                    LastTouchPoint = e.Location;
                }
                e.Handled = true;
                break;

            case SKTouchAction.Released:
                if (LastTouchPoint.HasValue)
                {
                    var distance = SKPoint.Distance(LastTouchPoint.Value, e.Location);
                    if (distance < 10) // Tap threshold
                    {
                        Tapped?.Invoke(e.Location);
                        OnTapped(e.Location);
                    }
                }
                LastTouchPoint = null;
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Called when user taps on the canvas. Override for custom tap handling.
    /// </summary>
    protected virtual void OnTapped(SKPoint location) { }

    #region Helper Methods

    /// <summary>
    /// Convert data coordinates to screen coordinates.
    /// </summary>
    protected SKPoint ToScreen(double x, double y, SKPoint center, float scale)
    {
        return new SKPoint(
            center.X + (float)x * scale,
            center.Y - (float)y * scale
        );
    }

    /// <summary>
    /// Convert screen coordinates to data coordinates.
    /// </summary>
    protected (double x, double y) FromScreen(SKPoint screen, SKPoint center, float scale)
    {
        return (
            (screen.X - center.X) / scale,
            (center.Y - screen.Y) / scale
        );
    }

    /// <summary>
    /// Draw text with background for readability.
    /// </summary>
    protected void DrawLabelWithBackground(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        SKPaint textPaint,
        SKColor? backgroundColor = null)
    {
        var bgColor = backgroundColor ?? VortexColors.Surface.WithAlpha(200);
        var textWidth = textPaint.MeasureText(text);
        var padding = 4f;

        using var bgPaint = new SKPaint { Color = bgColor, Style = SKPaintStyle.Fill };
        var rect = new SKRect(
            x - padding,
            y - textPaint.TextSize - padding,
            x + textWidth + padding,
            y + padding
        );

        canvas.DrawRoundRect(rect, 3, 3, bgPaint);
        canvas.DrawText(text, x, y, textPaint);
    }

    /// <summary>
    /// Draw an arrow from one point to another.
    /// </summary>
    protected void DrawArrow(SKCanvas canvas, SKPoint from, SKPoint to, SKPaint paint, float headSize = 10f)
    {
        canvas.DrawLine(from, to, paint);

        var angle = MathF.Atan2(to.Y - from.Y, to.X - from.X);
        var headAngle = 0.5f;

        var p1 = new SKPoint(
            to.X - headSize * MathF.Cos(angle - headAngle),
            to.Y - headSize * MathF.Sin(angle - headAngle));
        var p2 = new SKPoint(
            to.X - headSize * MathF.Cos(angle + headAngle),
            to.Y - headSize * MathF.Sin(angle + headAngle));

        canvas.DrawLine(to, p1, paint);
        canvas.DrawLine(to, p2, paint);
    }

    #endregion
}
