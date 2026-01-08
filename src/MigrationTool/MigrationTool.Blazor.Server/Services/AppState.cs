using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Blazor.Server.Services;

/// <summary>
/// Application state shared across components.
/// </summary>
public class AppState
{
    private SolutionInfo? _currentSolution;
    private string _workspacePath = string.Empty;

    /// <summary>
    /// Current loaded solution.
    /// </summary>
    public SolutionInfo? CurrentSolution
    {
        get => _currentSolution;
        set
        {
            _currentSolution = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Current workspace path.
    /// </summary>
    public string WorkspacePath
    {
        get => _workspacePath;
        set
        {
            _workspacePath = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Whether a solution is loaded.
    /// </summary>
    public bool HasSolution => CurrentSolution != null;

    /// <summary>
    /// Files selected in Explorer to be added to migration plan.
    /// </summary>
    public List<SourceFileInfo> PendingMigrationFiles { get; set; } = [];

    /// <summary>
    /// Whether there are pending files for migration.
    /// </summary>
    public bool HasPendingMigrationFiles => PendingMigrationFiles.Count > 0;

    /// <summary>
    /// Recently used paths for quick access.
    /// </summary>
    public List<string> RecentPaths { get; set; } = [];

    /// <summary>
    /// Event raised when state changes.
    /// </summary>
    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();
}
