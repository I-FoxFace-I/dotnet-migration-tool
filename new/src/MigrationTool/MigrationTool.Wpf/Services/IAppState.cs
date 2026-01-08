using System.ComponentModel;
using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Wpf.Services;

/// <summary>
/// Interface for shared application state.
/// Allows for easy mocking in unit tests.
/// </summary>
public interface IAppState : INotifyPropertyChanged
{
    /// <summary>
    /// Currently loaded solution.
    /// </summary>
    SolutionInfo? CurrentSolution { get; }

    /// <summary>
    /// Indicates if a solution is currently loaded.
    /// </summary>
    bool HasSolution { get; }

    /// <summary>
    /// Indicates if a loading operation is in progress.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Path to the workspace directory.
    /// </summary>
    string WorkspacePath { get; }

    /// <summary>
    /// Path to the solution file.
    /// </summary>
    string SolutionPath { get; }

    /// <summary>
    /// Error message from the last operation.
    /// </summary>
    string? ErrorMessage { get; }

    /// <summary>
    /// Loads a solution from the specified path.
    /// </summary>
    Task LoadSolutionAsync(string path);

    /// <summary>
    /// Refreshes the currently loaded solution.
    /// </summary>
    Task RefreshSolutionAsync();

    /// <summary>
    /// Clears the current solution.
    /// </summary>
    void ClearSolution();
}
