using AspireDesktop.ViewModels;

namespace AspireDesktop.Views;

public partial class ComparisonPage : ContentPage
{
    private ComparisonViewModel _viewModel = new();

    public ComparisonPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;

        // Update delta label when time changes
        _viewModel.Player.TimeChanged += UpdateDeltaLabel;
    }

    private void UpdateDeltaLabel()
    {
        if (!_viewModel.HasBothRuns) return;

        var metrics = _viewModel.GetCurrentMetrics();
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
        _viewModel.Player.TimeChanged -= UpdateDeltaLabel;
    }
}
