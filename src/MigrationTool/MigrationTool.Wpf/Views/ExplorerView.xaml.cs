using System.Windows.Controls;
using MigrationTool.Wpf.ViewModels;

namespace MigrationTool.Wpf.Views;

public partial class ExplorerView : UserControl
{
    public ExplorerView(ExplorerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
