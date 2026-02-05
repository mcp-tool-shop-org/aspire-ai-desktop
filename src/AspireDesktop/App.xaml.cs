using AspireDesktop.Services;
using AspireDesktop.ViewModels;

namespace AspireDesktop;

public partial class App : Application
{
    public static VortexSessionViewModel Session { get; } = new();
    public static KeyboardService Keyboard { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        Keyboard = new KeyboardService(Session);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        shell.BindingContext = Session;

        var window = new Window(shell)
        {
            Title = "ASPIRE Vortex Visualizer",
            Width = 1400,
            Height = 900
        };

        return window;
    }
}
