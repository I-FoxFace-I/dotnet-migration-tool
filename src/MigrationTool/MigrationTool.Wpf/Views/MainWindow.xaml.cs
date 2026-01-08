using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MigrationTool.Wpf.ViewModels;

namespace MigrationTool.Wpf.Views;

/// <summary>
/// Main application window with navigation.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        DataContext = viewModel;

        // Subscribe to page changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.CurrentPage))
            {
                UpdateContent();
            }
        };

        // Show initial page
        UpdateContent();
    }

    private void UpdateContent()
    {
        ContentArea.Content = _viewModel.CurrentPage switch
        {
            "Dashboard" => _serviceProvider.GetRequiredService<DashboardView>(),
            "Explorer" => _serviceProvider.GetRequiredService<ExplorerView>(),
            "Planner" => _serviceProvider.GetRequiredService<PlannerView>(),
            "Settings" => _serviceProvider.GetRequiredService<SettingsView>(),
            _ => _serviceProvider.GetRequiredService<DashboardView>()
        };
    }
}
