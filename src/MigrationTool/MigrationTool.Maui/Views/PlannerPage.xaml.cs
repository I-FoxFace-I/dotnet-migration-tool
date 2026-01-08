using MigrationTool.Maui.ViewModels;

namespace MigrationTool.Maui.Views;

public partial class PlannerPage : ContentPage
{
    public PlannerPage(PlannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
