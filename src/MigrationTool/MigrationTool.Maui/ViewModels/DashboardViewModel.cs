using CommunityToolkit.Mvvm.ComponentModel;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;
using MigrationTool.Maui.Services;

namespace MigrationTool.Maui.ViewModels;

/// <summary>
/// ViewModel for Dashboard page.
/// Displays solution statistics from shared AppState.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly AppState _appState;

    [ObservableProperty]
    private int _projectCount;

    [ObservableProperty]
    private int _testProjectCount;

    [ObservableProperty]
    private int _sourceProjectCount;

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private int _classCount;

    [ObservableProperty]
    private int _testCount;

    /// <summary>
    /// Gets the current solution from AppState.
    /// </summary>
    public SolutionInfo? Solution => _appState.CurrentSolution;

    /// <summary>
    /// Gets whether a solution is loaded.
    /// </summary>
    public bool HasSolution => _appState.HasSolution;

    /// <summary>
    /// Gets whether the solution is currently loading.
    /// </summary>
    public bool IsLoading => _appState.IsLoading;

    public DashboardViewModel(ILocalizationService localization, AppState appState) : base(localization)
    {
        _appState = appState;
        Title = T(Strings.DashboardTitle);

        // Subscribe to AppState changes
        _appState.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(AppState.CurrentSolution):
                    OnPropertyChanged(nameof(Solution));
                    UpdateStatistics();
                    break;
                case nameof(AppState.HasSolution):
                    OnPropertyChanged(nameof(HasSolution));
                    break;
                case nameof(AppState.IsLoading):
                    OnPropertyChanged(nameof(IsLoading));
                    break;
            }
        };

        // Initialize statistics
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        var solution = _appState.CurrentSolution;

        if (solution == null)
        {
            ProjectCount = 0;
            TestProjectCount = 0;
            SourceProjectCount = 0;
            FileCount = 0;
            ClassCount = 0;
            TestCount = 0;
            return;
        }

        ProjectCount = solution.ProjectCount;
        TestProjectCount = solution.TestProjectCount;
        SourceProjectCount = solution.SourceProjectCount;
        FileCount = solution.Projects.Sum(p => p.FileCount);
        ClassCount = solution.Projects.Sum(p => p.ClassCount);
        TestCount = solution.Projects.Sum(p => p.TestCount);
    }

    /// <summary>
    /// Updates statistics from a solution (for testing compatibility).
    /// </summary>
    public void UpdateFromSolution(SolutionInfo? solution)
    {
        if (solution == null)
        {
            ProjectCount = 0;
            TestProjectCount = 0;
            SourceProjectCount = 0;
            FileCount = 0;
            ClassCount = 0;
            TestCount = 0;
            return;
        }

        ProjectCount = solution.ProjectCount;
        TestProjectCount = solution.TestProjectCount;
        SourceProjectCount = solution.SourceProjectCount;
        FileCount = solution.Projects.Sum(p => p.FileCount);
        ClassCount = solution.Projects.Sum(p => p.ClassCount);
        TestCount = solution.Projects.Sum(p => p.TestCount);
    }
}
