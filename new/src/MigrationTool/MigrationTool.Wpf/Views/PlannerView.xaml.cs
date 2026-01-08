using System.Windows.Controls;
using MigrationTool.Wpf.ViewModels;

namespace MigrationTool.Wpf.Views;

public partial class PlannerView : UserControl
{
    public PlannerView(PlannerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
