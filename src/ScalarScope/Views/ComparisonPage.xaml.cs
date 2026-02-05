using ScalarScope.ViewModels;

namespace ScalarScope.Views;

public partial class ComparisonPage : ContentPage
{
    // Use the shared comparison instance from App
    private ComparisonViewModel ViewModel => App.Comparison;

    public ComparisonPage()
    {
        InitializeComponent();
        BindingContext = App.Comparison;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Update delta label when time changes
        ViewModel.Player.TimeChanged += UpdateDeltaLabel;
        // Trigger initial update if runs are loaded
        UpdateDeltaLabel();
    }

    private void UpdateDeltaLabel()
    {
        if (!ViewModel.HasBothRuns) return;

        var metrics = ViewModel.GetCurrentMetrics();
        var deltaSign = metrics.FirstFactorDelta >= 0 ? "+" : "";

        deltaLabel.Text = $"Δλ₁: {deltaSign}{metrics.FirstFactorDelta:P0}";

        // Color based on whether Path B shows improvement
        deltaLabel.TextColor = metrics.FirstFactorDelta > 0.1
            ? Color.FromArgb("#4ecdc4")  // Green - Path B better
            : metrics.FirstFactorDelta < -0.1
                ? Color.FromArgb("#ff6b6b")  // Red - Path A better
                : Color.FromArgb("#ffd93d"); // Yellow - similar
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        ViewModel.Player.TimeChanged -= UpdateDeltaLabel;
    }
}
