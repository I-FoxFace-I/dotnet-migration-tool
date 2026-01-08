using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Localization.Resources;
using MigrationTool.Wpf.Markup;
using MigrationTool.Wpf.Services;
using Wpf.ViewModels.Base;

namespace MigrationTool.Wpf.ViewModels;

/// <summary>
/// ViewModel for Dashboard view.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IAppState _appState;

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

    public SolutionInfo? Solution => _appState.CurrentSolution;
    public bool HasSolution => _appState.HasSolution;
    public bool IsLoading => _appState.IsLoading;

    public DashboardViewModel(
        ILogger<DashboardViewModel> logger,
        IAppState appState) : base(logger)
    {
        _appState = appState;
        DisplayName = L.Get(Strings.DashboardTitle);

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

        UpdateStatistics();
    }

    protected virtual void UpdateStatistics()
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
}
