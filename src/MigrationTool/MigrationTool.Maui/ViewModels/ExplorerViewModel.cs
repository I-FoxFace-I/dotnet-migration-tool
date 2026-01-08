using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;
using MigrationTool.Maui.Services;

namespace MigrationTool.Maui.ViewModels;

/// <summary>
/// ViewModel for Project Explorer page.
/// Displays projects and files from shared AppState.
/// </summary>
public partial class ExplorerViewModel : BaseViewModel
{
    private readonly AppState _appState;
    private readonly IProjectAnalyzer _projectAnalyzer;

    [ObservableProperty]
    private ProjectInfo? _selectedProject;

    [ObservableProperty]
    private SourceFileInfo? _selectedFile;

    public ObservableCollection<ProjectInfo> Projects { get; } = [];

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

    public ExplorerViewModel(
        ILocalizationService localization,
        AppState appState,
        IProjectAnalyzer projectAnalyzer) : base(localization)
    {
        _appState = appState;
        _projectAnalyzer = projectAnalyzer;
        Title = T(Strings.ExplorerTitle);

        // Subscribe to AppState changes
        _appState.PropertyChanged += async (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(AppState.CurrentSolution):
                    OnPropertyChanged(nameof(Solution));
                    await LoadProjectsAsync();
                    break;
                case nameof(AppState.HasSolution):
                    OnPropertyChanged(nameof(HasSolution));
                    break;
                case nameof(AppState.IsLoading):
                    OnPropertyChanged(nameof(IsLoading));
                    break;
            }
        };

        // Initialize if solution already loaded
        if (_appState.HasSolution)
        {
            _ = LoadProjectsAsync();
        }
    }

    private async Task LoadProjectsAsync()
    {
        Projects.Clear();
        SelectedProject = null;
        SelectedFile = null;

        var solution = _appState.CurrentSolution;
        if (solution == null)
            return;

        IsBusy = true;

        try
        {
            foreach (var project in solution.Projects)
            {
                // Enrich project with file information
                var enrichedProject = await _projectAnalyzer.EnrichProjectAsync(project);
                Projects.Add(enrichedProject);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await _appState.RefreshSolutionAsync();
    }

    [RelayCommand]
    private void SelectProject(ProjectInfo project)
    {
        SelectedProject = project;
        SelectedFile = null;
    }

    [RelayCommand]
    private void SelectFile(SourceFileInfo file)
    {
        SelectedFile = file;
    }

    [RelayCommand]
    private void ExpandAll()
    {
        // Handled in View
    }

    [RelayCommand]
    private void CollapseAll()
    {
        // Handled in View
    }
}
