using System.Windows.Controls;
using MigrationTool.Wpf.ViewModels;

namespace MigrationTool.Wpf.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
