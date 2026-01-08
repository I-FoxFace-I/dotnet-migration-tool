using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Wpf.Services;

/// <summary>
/// Shared application state for all ViewModels.
/// </summary>
public partial class AppState : ObservableObject, IAppState
{
    private readonly ISolutionAnalyzer? _solutionAnalyzer;

    [ObservableProperty]
    private SolutionInfo? _currentSolution;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private string _solutionPath = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasSolution => CurrentSolution != null;

    public AppState()
    {
    }

    public AppState(ISolutionAnalyzer solutionAnalyzer)
    {
        _solutionAnalyzer = solutionAnalyzer;
        Console.WriteLine($"[AppState] Initialized with SolutionAnalyzer");
    }

    partial void OnCurrentSolutionChanged(SolutionInfo? value)
    {
        Console.WriteLine($"[AppState] CurrentSolution changed to: {value?.Name ?? "null"}");
        OnPropertyChanged(nameof(HasSolution));
    }

    public async Task LoadSolutionAsync(string path)
    {
        Console.WriteLine($"[AppState] LoadSolutionAsync called: {path}");
        
        if (_solutionAnalyzer == null)
        {
            ErrorMessage = "Solution analyzer not available";
            Console.WriteLine($"[AppState] ❌ Solution analyzer not available");
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = null;
            SolutionPath = path;
            WorkspacePath = Path.GetDirectoryName(path) ?? string.Empty;

            Console.WriteLine($"[AppState] Analyzing solution...");
            CurrentSolution = await _solutionAnalyzer.AnalyzeSolutionAsync(path);
            Console.WriteLine($"[AppState] ✅ Solution loaded: {CurrentSolution?.Name} with {CurrentSolution?.ProjectCount} projects");
            
            if (CurrentSolution?.Projects != null)
            {
                foreach (var project in CurrentSolution.Projects)
                {
                    Console.WriteLine($"[AppState]   📁 {project.Name}: {project.SourceFiles.Count} files");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AppState] ❌ Error loading solution: {ex.Message}");
            ErrorMessage = ex.Message;
            CurrentSolution = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshSolutionAsync()
    {
        if (!string.IsNullOrEmpty(SolutionPath))
        {
            await LoadSolutionAsync(SolutionPath);
        }
    }

    public void ClearSolution()
    {
        CurrentSolution = null;
        SolutionPath = string.Empty;
        WorkspacePath = string.Empty;
        ErrorMessage = null;
    }
}
