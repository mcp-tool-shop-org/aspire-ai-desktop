namespace AspireDesktop.Views.Controls;

public partial class ToastNotification : ContentView
{
    private CancellationTokenSource? _hideCts;

    public ToastNotification()
    {
        InitializeComponent();
    }

    public async Task ShowAsync(string message, int durationMs = 1500)
    {
        // Cancel any pending hide
        _hideCts?.Cancel();
        _hideCts = new CancellationTokenSource();

        messageLabel.Text = message;
        toastBorder.IsVisible = true;

        // Fade in
        await toastBorder.FadeTo(1, 150);

        try
        {
            // Wait for duration
            await Task.Delay(durationMs, _hideCts.Token);

            // Fade out
            await toastBorder.FadeTo(0, 200);
            toastBorder.IsVisible = false;
        }
        catch (TaskCanceledException)
        {
            // New message coming, ignore hide
        }
    }
}
