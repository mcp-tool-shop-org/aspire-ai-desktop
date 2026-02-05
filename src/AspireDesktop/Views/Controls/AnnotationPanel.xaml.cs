namespace AspireDesktop.Views.Controls;

public partial class AnnotationPanel : ContentView
{
    public static readonly BindableProperty ShowPhasesProperty =
        BindableProperty.Create(nameof(ShowPhases), typeof(bool), typeof(AnnotationPanel), true,
            BindingMode.TwoWay);

    public static readonly BindableProperty ShowCurvatureWarningsProperty =
        BindableProperty.Create(nameof(ShowCurvatureWarnings), typeof(bool), typeof(AnnotationPanel), true,
            BindingMode.TwoWay);

    public static readonly BindableProperty ShowEigenInsightsProperty =
        BindableProperty.Create(nameof(ShowEigenInsights), typeof(bool), typeof(AnnotationPanel), true,
            BindingMode.TwoWay);

    public static readonly BindableProperty ShowFailureMarkersProperty =
        BindableProperty.Create(nameof(ShowFailureMarkers), typeof(bool), typeof(AnnotationPanel), true,
            BindingMode.TwoWay);

    public bool ShowPhases
    {
        get => (bool)GetValue(ShowPhasesProperty);
        set => SetValue(ShowPhasesProperty, value);
    }

    public bool ShowCurvatureWarnings
    {
        get => (bool)GetValue(ShowCurvatureWarningsProperty);
        set => SetValue(ShowCurvatureWarningsProperty, value);
    }

    public bool ShowEigenInsights
    {
        get => (bool)GetValue(ShowEigenInsightsProperty);
        set => SetValue(ShowEigenInsightsProperty, value);
    }

    public bool ShowFailureMarkers
    {
        get => (bool)GetValue(ShowFailureMarkersProperty);
        set => SetValue(ShowFailureMarkersProperty, value);
    }

    public AnnotationPanel()
    {
        InitializeComponent();
    }
}
