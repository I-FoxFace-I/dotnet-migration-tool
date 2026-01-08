using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MigrationTool.Localization.Resources;
using MigrationTool.Wpf.Markup;
using MigrationTool.Wpf.Services;
using Wpf.ViewModels.Base;

namespace MigrationTool.Wpf.ViewModels;

/// <summary>
/// Main window ViewModel with navigation.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly AppState _appState;

    [ObservableProperty]
    private string _currentPage = "Dashboard";

    public MainViewModel(
        ILogger<MainViewModel> logger,
        AppState appState) : base(logger)
    {
        _appState = appState;
        DisplayName = L.Get(Strings.AppTitle);
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPage = page;
        Logger.LogDebug("Navigated to {Page}", page);
    }

    [RelayCommand]
    private void NavigateToDashboard() => NavigateTo("Dashboard");

    [RelayCommand]
    private void NavigateToExplorer() => NavigateTo("Explorer");

    [RelayCommand]
    private void NavigateToPlanner() => NavigateTo("Planner");

    [RelayCommand]
    private void NavigateToSettings() => NavigateTo("Settings");
}
