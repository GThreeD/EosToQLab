namespace EosToQLab.Application;

public partial class Application
{
    public Application()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new(new MainPage())
        {
            Title = "EosToQLab",
            Width = 1180,
            Height = 820,
            MinimumWidth = 900,
            MinimumHeight = 640
        };
    }
}