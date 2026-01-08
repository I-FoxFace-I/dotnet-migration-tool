using System.Windows.Controls;
using MigrationTool.Wpf.ViewModels;

namespace MigrationTool.Wpf.Views;

public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
