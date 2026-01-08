using MigrationTool.Maui.ViewModels;

namespace MigrationTool.Maui.Views;

public partial class ExplorerPage : ContentPage
{
    public ExplorerPage(ExplorerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
