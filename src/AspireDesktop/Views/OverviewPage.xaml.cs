using AspireDesktop.Services;
using AspireDesktop.ViewModels;

namespace AspireDesktop.Views;

public partial class OverviewPage : ContentPage
{
    private readonly ExportService _exportService = new();
    private readonly ExportViewModel _exportViewModel = new();

    public OverviewPage()
    {
        InitializeComponent();
        BindingContext = App.Session;
    }

    private async void OnQuickExportClicked(object? sender, EventArgs e)
    {
        if (App.Session.Run == null)
        {
            exportStatusLabel.Text = "No run loaded";
            return;
        }

        try
        {
            exportStatusLabel.Text = "Exporting...";
            exportStatusLabel.TextColor = Color.FromArgb("#888");

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var aspireExports = Path.Combine(documentsPath, "ASPIRE Exports");
            Directory.CreateDirectory(aspireExports);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputPath = Path.Combine(aspireExports, $"aspire_screenshot_{timestamp}.png");

            await _exportService.ExportStillAsync(
                App.Session.Run,
                App.Session.Player.Time,
                outputPath);

            exportStatusLabel.Text = $"Saved: {Path.GetFileName(outputPath)}";
            exportStatusLabel.TextColor = Color.FromArgb("#4ecdc4");
        }
        catch (Exception ex)
        {
            exportStatusLabel.Text = $"Error: {ex.Message}";
            exportStatusLabel.TextColor = Color.FromArgb("#ff6b6b");
        }
    }

    private async void OnExportSettingsClicked(object? sender, EventArgs e)
    {
        // For now, show a simple dialog. In future, this could be a full settings page.
        var action = await DisplayActionSheet(
            "Export Options",
            "Cancel",
            null,
            "Screenshot (1920x1080)",
            "Screenshot (3840x2160 4K)",
            "Frame Sequence (5s @ 30fps)",
            "Frame Sequence (10s @ 60fps)");

        if (action == null || action == "Cancel" || App.Session.Run == null) return;

        try
        {
            exportStatusLabel.Text = "Exporting...";

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var aspireExports = Path.Combine(documentsPath, "ASPIRE Exports");
            Directory.CreateDirectory(aspireExports);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (action.StartsWith("Screenshot"))
            {
                var is4K = action.Contains("4K");
                var outputPath = Path.Combine(aspireExports, $"aspire_{(is4K ? "4k" : "hd")}_{timestamp}.png");

                await _exportService.ExportStillAsync(
                    App.Session.Run,
                    App.Session.Player.Time,
                    outputPath,
                    new ExportOptions
                    {
                        Width = is4K ? 3840 : 1920,
                        Height = is4K ? 2160 : 1080
                    });

                exportStatusLabel.Text = $"Saved: {Path.GetFileName(outputPath)}";
            }
            else if (action.StartsWith("Frame Sequence"))
            {
                var is60fps = action.Contains("60fps");
                var is10s = action.Contains("10s");
                var outputDir = Path.Combine(aspireExports, $"sequence_{timestamp}");

                var paths = await _exportService.ExportFrameSequenceAsync(
                    App.Session.Run,
                    outputDir,
                    new ExportOptions
                    {
                        Fps = is60fps ? 60 : 30,
                        Duration = is10s ? 10.0 : 5.0
                    });

                exportStatusLabel.Text = $"Saved {paths.Length} frames to: sequence_{timestamp}";
            }

            exportStatusLabel.TextColor = Color.FromArgb("#4ecdc4");
        }
        catch (Exception ex)
        {
            exportStatusLabel.Text = $"Error: {ex.Message}";
            exportStatusLabel.TextColor = Color.FromArgb("#ff6b6b");
        }
    }
}
