using AspireDesktop.Views;

namespace AspireDesktop;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes
        Routing.RegisterRoute("recovery", typeof(RecoveryPage));

        // Check if we need to show recovery page
        if (App.NeedsRecovery)
        {
            // Navigate to recovery page after shell is loaded
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(100); // Let the shell initialize
                await GoToAsync("recovery");
            });
        }
    }
}
