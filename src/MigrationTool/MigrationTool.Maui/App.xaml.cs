namespace MigrationTool.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "Migration Tool",
            Width = 1400,
            Height = 900
        };
    }
}
