namespace ScalarScope.Views;

public partial class FailuresPage : ContentPage
{
    public FailuresPage()
    {
        InitializeComponent();
        BindingContext = App.Session;
    }
}
