namespace ScalarScope.Views;

public partial class TrajectoryPage : ContentPage
{
    public TrajectoryPage()
    {
        InitializeComponent();
        BindingContext = App.Session;
    }

    private void OnResetViewClicked(object? sender, EventArgs e)
    {
        trajectoryCanvas.ResetView();
    }
}
