using System.Collections.ObjectModel;
using FluentAssertions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization;
using Moq;
using Xunit;

namespace MigrationTool.Maui.Tests.ViewModels;

/// <summary>
/// Tests for ExplorerViewModel - project and file browser.
/// </summary>
public class ExplorerViewModelTests
{
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly Mock<IProjectAnalyzer> _projectAnalyzerMock;

    public ExplorerViewModelTests()
    {
        _localizationMock = new Mock<ILocalizationService>();
        _localizationMock.Setup(x => x.Get(It.IsAny<string>())).Returns((string key) => key);

        _projectAnalyzerMock = new Mock<IProjectAnalyzer>();
    }

    private TestableExplorerViewModel CreateViewModel()
    {
        return new TestableExplorerViewModel(_localizationMock.Object, _projectAnalyzerMock.Object);
    }

    [Fact]
    public void Constructor_SetsTitle()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Projects.Should().BeEmpty();
        viewModel.Solution.Should().BeNull();
        viewModel.SelectedProject.Should().BeNull();
        viewModel.SelectedFile.Should().BeNull();
    }

    [Fact]
    public async Task LoadSolutionAsync_WithValidSolution_LoadsProjects()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var projects = new List<ProjectInfo>
        {
            CreateProject("Project1"),
            CreateProject("Project2"),
        };
        var solution = new SolutionInfo { Name = "TestSolution", Path = @"C:\test\solution.sln", Projects = projects };

        _projectAnalyzerMock
            .Setup(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectInfo p, CancellationToken _) => p);

        // Act
        await viewModel.LoadSolutionAsync(solution);

        // Assert
        viewModel.Solution.Should().Be(solution);
        viewModel.Projects.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadSolutionAsync_WithNullSolution_ClearsProjects()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Projects.Add(CreateProject("Existing"));

        // Act
        await viewModel.LoadSolutionAsync(null);

        // Assert
        viewModel.Solution.Should().BeNull();
        viewModel.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSolutionAsync_SetsIsBusyDuringLoad()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var busyStates = new List<bool>();
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsBusy))
                busyStates.Add(viewModel.IsBusy);
        };

        var projects = new List<ProjectInfo> { CreateProject("Project1") };
        var solution = new SolutionInfo { Name = "TestSolution", Path = @"C:\test\solution.sln", Projects = projects };

        var tcs = new TaskCompletionSource<ProjectInfo>();
        _projectAnalyzerMock
            .Setup(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var loadTask = viewModel.LoadSolutionAsync(solution);

        // Assert - should be busy during load
        viewModel.IsBusy.Should().BeTrue();

        // Complete
        tcs.SetResult(projects[0]);
        await loadTask;

        // Assert - should not be busy after load
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSolutionAsync_EnrichesEachProject()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var projects = new List<ProjectInfo>
        {
            CreateProject("Project1"),
            CreateProject("Project2"),
            CreateProject("Project3"),
        };
        var solution = new SolutionInfo { Name = "TestSolution", Path = @"C:\test\solution.sln", Projects = projects };

        _projectAnalyzerMock
            .Setup(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectInfo p, CancellationToken _) => p);

        // Act
        await viewModel.LoadSolutionAsync(solution);

        // Assert
        _projectAnalyzerMock.Verify(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public void SelectProject_SetsSelectedProject()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = CreateProject("TestProject");

        // Act
        viewModel.SelectProjectCommand.Execute(project);

        // Assert
        viewModel.SelectedProject.Should().Be(project);
    }

    [Fact]
    public void SelectProject_ClearsSelectedFile()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var file = CreateSourceFile("Test.cs");
        viewModel.SetSelectedFile(file);

        var project = CreateProject("TestProject");

        // Act
        viewModel.SelectProjectCommand.Execute(project);

        // Assert
        viewModel.SelectedFile.Should().BeNull();
    }

    [Fact]
    public void SelectFile_SetsSelectedFile()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var file = CreateSourceFile("Test.cs");

        // Act
        viewModel.SelectFileCommand.Execute(file);

        // Assert
        viewModel.SelectedFile.Should().Be(file);
    }

    [Fact]
    public void ExpandAll_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = () => viewModel.ExpandAllCommand.Execute(null);
        act.Should().NotThrow();
    }

    [Fact]
    public void CollapseAll_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = () => viewModel.CollapseAllCommand.Execute(null);
        act.Should().NotThrow();
    }

    private static ProjectInfo CreateProject(string name)
    {
        return new ProjectInfo
        {
            Name = name,
            Path = $@"C:\test\{name}\{name}.csproj",
            IsTestProject = false,
            TargetFramework = "net9.0"
        };
    }

    private static SourceFileInfo CreateSourceFile(string name)
    {
        return new SourceFileInfo
        {
            Name = name,
            Path = $@"C:\test\{name}",
            Namespace = "Test.Namespace"
        };
    }
}

/// <summary>
/// Testable version of ExplorerViewModel.
/// </summary>
public class TestableExplorerViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly IProjectAnalyzer _projectAnalyzer;
    private readonly ILocalizationService _localization;

    private bool _isBusy;
    private string _title = string.Empty;
    private SolutionInfo? _solution;
    private ProjectInfo? _selectedProject;
    private SourceFileInfo? _selectedFile;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public SolutionInfo? Solution
    {
        get => _solution;
        set => SetProperty(ref _solution, value);
    }

    public ProjectInfo? SelectedProject
    {
        get => _selectedProject;
        set => SetProperty(ref _selectedProject, value);
    }

    public SourceFileInfo? SelectedFile
    {
        get => _selectedFile;
        set => SetProperty(ref _selectedFile, value);
    }

    public ObservableCollection<ProjectInfo> Projects { get; } = [];

    public CommunityToolkit.Mvvm.Input.RelayCommand<ProjectInfo> SelectProjectCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand<SourceFileInfo> SelectFileCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand ExpandAllCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand CollapseAllCommand { get; }

    public TestableExplorerViewModel(ILocalizationService localization, IProjectAnalyzer projectAnalyzer)
    {
        _localization = localization;
        _projectAnalyzer = projectAnalyzer;
        Title = _localization.Get("ExplorerTitle");

        SelectProjectCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<ProjectInfo>(SelectProject);
        SelectFileCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<SourceFileInfo>(SelectFile);
        ExpandAllCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ExpandAll);
        CollapseAllCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(CollapseAll);
    }

    public async Task LoadSolutionAsync(SolutionInfo? solution)
    {
        Solution = solution;
        Projects.Clear();

        if (solution == null)
            return;

        IsBusy = true;

        try
        {
            foreach (var project in solution.Projects)
            {
                var enrichedProject = await _projectAnalyzer.EnrichProjectAsync(project);
                Projects.Add(enrichedProject);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SelectProject(ProjectInfo? project)
    {
        SelectedProject = project;
        SelectedFile = null;
    }

    private void SelectFile(SourceFileInfo? file)
    {
        SelectedFile = file;
    }

    private void ExpandAll() { /* Handled in View */ }
    private void CollapseAll() { /* Handled in View */ }

    // Test helpers
    public void SetSelectedFile(SourceFileInfo? file) => SelectedFile = file;
}
