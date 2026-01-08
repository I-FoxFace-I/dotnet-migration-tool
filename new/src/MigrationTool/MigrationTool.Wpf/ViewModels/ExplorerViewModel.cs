using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization.Resources;
using MigrationTool.Wpf.Markup;
using MigrationTool.Wpf.Services;
using Wpf.ViewModels.Base;

namespace MigrationTool.Wpf.ViewModels;

/// <summary>
/// ViewModel for Explorer view.
/// </summary>
public partial class ExplorerViewModel : BaseViewModel
{
    private readonly IAppState _appState;
    private readonly IProjectAnalyzer _projectAnalyzer;

    [ObservableProperty]
    private ObservableCollection<ProjectInfo> _projects = [];

    [ObservableProperty]
    private ObservableCollection<SourceFileInfo> _files = [];

    [ObservableProperty]
    private ProjectInfo? _selectedProject;

    [ObservableProperty]
    private SourceFileInfo? _selectedFile;

    [ObservableProperty]
    private string _selectedViewMode = "Tree";

    public bool HasSolution => _appState.HasSolution;
    public bool IsLoading => _appState.IsLoading;

    public ExplorerViewModel(
        ILogger<ExplorerViewModel> logger,
        IAppState appState,
        IProjectAnalyzer projectAnalyzer) : base(logger)
    {
        _appState = appState;
        _projectAnalyzer = projectAnalyzer;
        DisplayName = L.Get(Strings.ExplorerTitle);

        Console.WriteLine($"[ExplorerViewModel] Initialized, HasSolution: {_appState.HasSolution}");

        _appState.PropertyChanged += (s, e) =>
        {
            Console.WriteLine($"[ExplorerViewModel] AppState.{e.PropertyName} changed");
            switch (e.PropertyName)
            {
                case nameof(AppState.CurrentSolution):
                    _ = LoadProjectsAsync();
                    break;
                case nameof(AppState.HasSolution):
                    OnPropertyChanged(nameof(HasSolution));
                    break;
                case nameof(AppState.IsLoading):
                    OnPropertyChanged(nameof(IsLoading));
                    break;
            }
        };

        _ = LoadProjectsAsync();
    }

    protected virtual async Task LoadProjectsAsync()
    {
        Console.WriteLine($"[ExplorerViewModel] LoadProjects called");
        Projects.Clear();
        Files.Clear();

        if (_appState.CurrentSolution?.Projects != null)
        {
            Console.WriteLine($"[ExplorerViewModel] Loading {_appState.CurrentSolution.Projects.Count} projects");
            
            // Enrich each project with source files
            foreach (var project in _appState.CurrentSolution.Projects)
            {
                try
                {
                    var enrichedProject = await _projectAnalyzer.EnrichProjectAsync(project);
                    Console.WriteLine($"[ExplorerViewModel]   - {enrichedProject.Name} ({enrichedProject.SourceFiles.Count} files)");
                    Projects.Add(enrichedProject);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to enrich project: {ProjectName}", project.Name);
                    // Add project even if enrichment fails
                    Projects.Add(project);
                }
            }
        }
        else
        {
            Console.WriteLine($"[ExplorerViewModel] No solution or projects available");
        }
    }

    partial void OnSelectedProjectChanged(ProjectInfo? value)
    {
        Console.WriteLine($"[ExplorerViewModel] SelectedProject changed to: {value?.Name ?? "null"}");
        LoadFiles();
    }

    protected virtual void LoadFiles()
    {
        Files.Clear();
        SelectedFile = null;

        if (SelectedProject?.SourceFiles != null)
        {
            Console.WriteLine($"[ExplorerViewModel] Loading {SelectedProject.SourceFiles.Count} files for {SelectedProject.Name}");
            foreach (var file in SelectedProject.SourceFiles)
            {
                Console.WriteLine($"[ExplorerViewModel]   - {file.Name}");
                Files.Add(file);
            }
        }
        else
        {
            Console.WriteLine($"[ExplorerViewModel] No files to load (SelectedProject is null or has no files)");
        }
    }

    [RelayCommand]
    private void SelectProject(ProjectInfo? project)
    {
        Console.WriteLine($"[ExplorerViewModel] SelectProject command: {project?.Name ?? "null"}");
        SelectedProject = project;
    }

    [RelayCommand]
    private void SelectFile(SourceFileInfo? file)
    {
        Console.WriteLine($"[ExplorerViewModel] SelectFile command: {file?.Name ?? "null"}");
        SelectedFile = file;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Console.WriteLine($"[ExplorerViewModel] RefreshAsync called");
        await _appState.RefreshSolutionAsync();
    }
}
