using CommunityToolkit.Mvvm.ComponentModel;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Maui.Services;

/// <summary>
/// Shared application state accessible by all ViewModels.
/// Singleton service that maintains current solution and workspace information.
/// </summary>
public partial class AppState : ObservableObject
{
    private readonly ISolutionAnalyzer _solutionAnalyzer;

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private string _solutionPath = string.Empty;

    [ObservableProperty]
    private SolutionInfo? _currentSolution;

    [ObservableProperty]
    private bool _hasSolution;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public AppState(ISolutionAnalyzer solutionAnalyzer)
    {
        _solutionAnalyzer = solutionAnalyzer;
    }

    /// <summary>
    /// Loads a solution from the specified path.
    /// </summary>
    public async Task LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            ErrorMessage = "Solution path cannot be empty";
            return;
        }

        if (!File.Exists(solutionPath))
        {
            ErrorMessage = $"Solution file not found: {solutionPath}";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            SolutionPath = solutionPath;
            WorkspacePath = Path.GetDirectoryName(solutionPath) ?? string.Empty;

            CurrentSolution = await _solutionAnalyzer.AnalyzeSolutionAsync(solutionPath, cancellationToken);
            HasSolution = CurrentSolution != null;

            if (CurrentSolution == null)
            {
                ErrorMessage = "Failed to analyze solution";
            }
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Operation was cancelled";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading solution: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error loading solution: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Clears the current solution.
    /// </summary>
    public void ClearSolution()
    {
        CurrentSolution = null;
        HasSolution = false;
        SolutionPath = string.Empty;
        ErrorMessage = null;
    }

    /// <summary>
    /// Refreshes the current solution.
    /// </summary>
    public async Task RefreshSolutionAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(SolutionPath))
        {
            await LoadSolutionAsync(SolutionPath, cancellationToken);
        }
    }
}
