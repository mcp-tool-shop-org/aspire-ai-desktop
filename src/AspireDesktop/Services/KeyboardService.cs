using AspireDesktop.ViewModels;

namespace AspireDesktop.Services;

/// <summary>
/// Handles global keyboard shortcuts for the application.
/// Note: Full keyboard support requires platform-specific implementation.
/// This service provides the action handlers that can be wired to platform events.
/// </summary>
public class KeyboardService
{
    private readonly VortexSessionViewModel _session;
    private readonly ExportService _exportService;

    public event Action<string>? ShortcutTriggered;

    public KeyboardService(VortexSessionViewModel session)
    {
        _session = session;
        _exportService = new ExportService();
    }

    /// <summary>
    /// Handle a key press. Returns true if handled.
    /// </summary>
    public bool HandleKeyPress(string key, bool ctrl = false, bool shift = false)
    {
        // Playback controls
        if (key == "Space")
        {
            _session.Player.PlayPauseCommand.Execute(null);
            ShortcutTriggered?.Invoke("Play/Pause");
            return true;
        }

        if (key == "Left")
        {
            _session.Player.StepBackwardCommand.Execute(null);
            ShortcutTriggered?.Invoke("Step Back");
            return true;
        }

        if (key == "Right")
        {
            _session.Player.StepForwardCommand.Execute(null);
            ShortcutTriggered?.Invoke("Step Forward");
            return true;
        }

        if (key == "Home")
        {
            _session.Player.JumpToTimeCommand.Execute(0.0);
            ShortcutTriggered?.Invoke("Jump to Start");
            return true;
        }

        if (key == "End")
        {
            _session.Player.JumpToTimeCommand.Execute(1.0);
            ShortcutTriggered?.Invoke("Jump to End");
            return true;
        }

        // Speed controls
        if (key == "OemPlus" || key == "Add")
        {
            _session.Player.IncreaseSpeed();
            ShortcutTriggered?.Invoke($"Speed: {GetSpeedLabel(_session.Player.SpeedIndex)}");
            return true;
        }

        if (key == "OemMinus" || key == "Subtract")
        {
            _session.Player.DecreaseSpeed();
            ShortcutTriggered?.Invoke($"Speed: {GetSpeedLabel(_session.Player.SpeedIndex)}");
            return true;
        }

        // Export shortcut
        if (key == "S" && !ctrl)
        {
            _ = QuickExportAsync();
            return true;
        }

        // Ctrl+S for save/export dialog
        if (key == "S" && ctrl)
        {
            _ = QuickExportAsync();
            ShortcutTriggered?.Invoke("Screenshot saved");
            return true;
        }

        // Tab navigation (1-6)
        if (key is "D1" or "D2" or "D3" or "D4" or "D5" or "D6")
        {
            var tabIndex = int.Parse(key[1..]) - 1;
            NavigateToTab(tabIndex);
            return true;
        }

        return false;
    }

    private async Task QuickExportAsync()
    {
        if (_session.Run == null) return;

        try
        {
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var aspireExports = Path.Combine(documentsPath, "ASPIRE Exports");
            Directory.CreateDirectory(aspireExports);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputPath = Path.Combine(aspireExports, $"aspire_quick_{timestamp}.png");

            await _exportService.ExportStillAsync(_session.Run, _session.Player.Time, outputPath);

            ShortcutTriggered?.Invoke($"Saved: {Path.GetFileName(outputPath)}");
        }
        catch (Exception ex)
        {
            ShortcutTriggered?.Invoke($"Export failed: {ex.Message}");
        }
    }

    private static void NavigateToTab(int index)
    {
        var routes = new[] { "overview", "trajectory", "scalars", "geometry", "compare", "failures" };
        if (index >= 0 && index < routes.Length)
        {
            Shell.Current.GoToAsync($"//{routes[index]}");
        }
    }

    private static string GetSpeedLabel(int speedIndex) => speedIndex switch
    {
        0 => "0.25x",
        1 => "0.5x",
        2 => "1x",
        3 => "2x",
        4 => "4x",
        _ => "1x"
    };
}
