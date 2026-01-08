using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;

namespace MigrationTool.Maui.ViewModels;

/// <summary>
/// Main ViewModel managing application state.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly ISolutionAnalyzer _solutionAnalyzer;

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private SolutionInfo? _currentSolution;

    [ObservableProperty]
    private bool _hasSolution;

    public MainViewModel(
        ILocalizationService localization,
        ISolutionAnalyzer solutionAnalyzer) : base(localization)
    {
        _solutionAnalyzer = solutionAnalyzer;
        Title = T(Strings.AppTitle);
    }

    [RelayCommand]
    private async Task LoadSolutionAsync(string solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return;

        try
        {
            IsBusy = true;
            CurrentSolution = await _solutionAnalyzer.AnalyzeSolutionAsync(solutionPath);
            HasSolution = CurrentSolution != null;
            WorkspacePath = Path.GetDirectoryName(solutionPath) ?? string.Empty;
        }
        catch (Exception ex)
        {
            // TODO: Show error dialog
            System.Diagnostics.Debug.WriteLine($"Error loading solution: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearSolution()
    {
        CurrentSolution = null;
        HasSolution = false;
    }
}
