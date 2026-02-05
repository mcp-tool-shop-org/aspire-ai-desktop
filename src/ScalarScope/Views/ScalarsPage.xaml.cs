namespace ScalarScope.Views;

public partial class ScalarsPage : ContentPage
{
    public ScalarsPage()
    {
        InitializeComponent();
        BindingContext = App.Session;
    }
}
