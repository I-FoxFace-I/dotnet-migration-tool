using Microsoft.UI.Xaml;

namespace MigrationTool.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MigrationTool.Maui.MauiProgram.CreateMauiApp();
}
