using System.Collections.ObjectModel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Wpf.Services;
using MigrationTool.Wpf.Tests.TestHelpers;
using MigrationTool.Wpf.ViewModels;
using Moq;
using Xunit;

namespace MigrationTool.Wpf.Tests.ViewModels;

/// <summary>
/// Unit tests for ExplorerViewModel - project and file browser.
/// Uses TestableExplorerViewModel which inherits from real ViewModel.
/// </summary>
public class ExplorerViewModelTests
{
    #region Test Constants

    private static class PropertyNames
    {
        public const string Projects = "Projects";
        public const string Files = "Files";
        public const string SelectedProject = "SelectedProject";
        public const string SelectedFile = "SelectedFile";
        public const string SelectedViewMode = "SelectedViewMode";
        public const string HasSolution = "HasSolution";
        public const string IsLoading = "IsLoading";
    }

    private static class ViewModes
    {
        public const string Tree = "Tree";
        public const string List = "List";
        public const string Classes = "Classes";
    }

    #endregion

    private readonly Mock<ILogger<TestableExplorerViewModel>> _loggerMock;
    private readonly Mock<IAppState> _appStateMock;
    private readonly Mock<IProjectAnalyzer> _projectAnalyzerMock;

    public ExplorerViewModelTests()
    {
        _loggerMock = new Mock<ILogger<TestableExplorerViewModel>>();
        _appStateMock = new Mock<IAppState>();
        _projectAnalyzerMock = new Mock<IProjectAnalyzer>();
        
        // Setup default behavior - return project as-is (no enrichment needed for most tests)
        _projectAnalyzerMock
            .Setup(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectInfo project, CancellationToken _) => project);
    }

    private TestableExplorerViewModel CreateViewModel(SolutionInfo? initialSolution = null)
    {
        _appStateMock.Setup(x => x.CurrentSolution).Returns(initialSolution);
        _appStateMock.Setup(x => x.HasSolution).Returns(initialSolution != null);
        _appStateMock.Setup(x => x.IsLoading).Returns(false);

        return new TestableExplorerViewModel(_loggerMock.Object, _appStateMock.Object, _projectAnalyzerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDisplayName()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_InitializesEmptyCollections()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Projects.Should().NotBeNull();
        viewModel.Projects.Should().BeEmpty();
        viewModel.Files.Should().NotBeNull();
        viewModel.Files.Should().BeEmpty();
        viewModel.SelectedProject.Should().BeNull();
        viewModel.SelectedFile.Should().BeNull();
    }

    [Fact]
    public void Constructor_SetsDefaultViewMode()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SelectedViewMode.Should().Be(ViewModes.Tree);
    }

    [Fact]
    public void Constructor_WithExistingSolution_LoadsProjects()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();

        // Act
        var viewModel = CreateViewModel(solution);
        
        // Allow async loading to complete
        Task.Delay(100).Wait();

        // Assert
        viewModel.Projects.Should().HaveCount(3);
    }

    #endregion

    #region LoadProjects Tests

    [Fact]
    public async Task LoadProjects_WithValidSolution_PopulatesProjectsList()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestDataFactory.CreateDefaultSolution();

        // Act
        await viewModel.SimulateAppStateChange(solution);

        // Assert
        viewModel.Projects.Should().HaveCount(3);
        viewModel.Projects.Select(p => p.Name).Should().Contain(TestDataFactory.ProjectNames.Proj1);
        viewModel.Projects.Select(p => p.Name).Should().Contain(TestDataFactory.ProjectNames.Proj2);
        viewModel.Projects.Select(p => p.Name).Should().Contain(TestDataFactory.ProjectNames.Proj1Tests);
    }

    [Fact]
    public async Task LoadProjects_WithNullSolution_ClearsProjectsList()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        _appStateMock.Setup(x => x.CurrentSolution).Returns(solution);
        _appStateMock.Setup(x => x.HasSolution).Returns(true);
        var viewModel = new TestableExplorerViewModel(_loggerMock.Object, _appStateMock.Object, _projectAnalyzerMock.Object);

        // Act
        await viewModel.SimulateAppStateChange(null);

        // Assert
        viewModel.Projects.Should().BeEmpty();
        viewModel.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadProjects_WithEmptySolution_HasEmptyProjectsList()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestDataFactory.CreateEmptySolution();

        // Act
        await viewModel.SimulateAppStateChange(solution);

        // Assert
        viewModel.Projects.Should().BeEmpty();
    }

    #endregion

    #region SelectProject Tests

    [Fact]
    public void SelectProject_WithValidProject_LoadsFiles()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        var project = viewModel.Projects.First();

        // Act
        viewModel.SelectedProject = project;

        // Assert
        viewModel.SelectedProject.Should().Be(project);
        viewModel.Files.Should().NotBeEmpty();
        viewModel.Files.Should().HaveCount(project.SourceFiles.Count);
    }

    [Fact]
    public void SelectProject_WithNull_ClearsFiles()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        viewModel.SelectedProject = viewModel.Projects.First();

        // Act
        viewModel.SelectedProject = null;

        // Assert
        viewModel.SelectedProject.Should().BeNull();
        viewModel.Files.Should().BeEmpty();
    }

    [Fact]
    public void SelectProject_ClearsSelectedFile()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        viewModel.SelectedProject = viewModel.Projects.First();
        viewModel.SelectedFile = viewModel.Files.First();

        // Act
        viewModel.SelectedProject = viewModel.Projects.Last();

        // Assert
        viewModel.SelectedFile.Should().BeNull();
    }

    #endregion

    #region SelectFile Tests

    [Fact]
    public void SelectFile_WithValidFile_SetsSelectedFile()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        viewModel.SelectedProject = viewModel.Projects.First();
        var file = viewModel.Files.First();

        // Act
        viewModel.SelectedFile = file;

        // Assert
        viewModel.SelectedFile.Should().Be(file);
    }

    [Fact]
    public void SelectFile_WithNull_ClearsSelectedFile()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        viewModel.SelectedProject = viewModel.Projects.First();
        viewModel.SelectedFile = viewModel.Files.First();

        // Act
        viewModel.SelectedFile = null;

        // Assert
        viewModel.SelectedFile.Should().BeNull();
    }

    #endregion

    #region SetViewMode Tests

    [Fact]
    public void SetViewMode_ChangesSelectedViewMode()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedViewMode = ViewModes.List;

        // Assert
        viewModel.SelectedViewMode.Should().Be(ViewModes.List);
    }

    [Theory]
    [InlineData(ViewModes.Tree)]
    [InlineData(ViewModes.List)]
    [InlineData(ViewModes.Classes)]
    public void SetViewMode_AcceptsAllValidModes(string mode)
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SelectedViewMode = mode;

        // Assert
        viewModel.SelectedViewMode.Should().Be(mode);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void SelectProject_RaisesPropertyChangedEvent()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.SelectedProject = viewModel.Projects.First();

        // Assert
        changedProperties.Should().Contain(PropertyNames.SelectedProject);
    }

    [Fact]
    public void SelectFile_RaisesPropertyChangedEvent()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();
        var viewModel = CreateViewModel(solution);
        viewModel.SelectedProject = viewModel.Projects.First();

        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.SelectedFile = viewModel.Files.First();

        // Assert
        changedProperties.Should().Contain(PropertyNames.SelectedFile);
    }

    #endregion
}

/// <summary>
/// Testable version of ExplorerViewModel - inherits from real ViewModel and adds test helpers.
/// </summary>
public class TestableExplorerViewModel : ExplorerViewModel
{
    private SolutionInfo? _simulatedSolution;
    private bool _isSimulating;

    public TestableExplorerViewModel(
        ILogger<TestableExplorerViewModel> logger, 
        IAppState appState,
        MigrationTool.Core.Abstractions.Services.IProjectAnalyzer projectAnalyzer)
        : base(logger, appState, projectAnalyzer)
    {
    }

    /// <summary>
    /// Simulates AppState.CurrentSolution change for testing purposes.
    /// </summary>
    public async Task SimulateAppStateChange(SolutionInfo? solution)
    {
        _simulatedSolution = solution;
        _isSimulating = true;
        await LoadProjectsAsync();
        _isSimulating = false;
    }

    protected override async Task LoadProjectsAsync()
    {
        if (_isSimulating)
        {
            // Use simulated solution (synchronously for tests)
            Projects.Clear();
            Files.Clear();

            if (_simulatedSolution?.Projects != null)
            {
                foreach (var project in _simulatedSolution.Projects)
                {
                    Projects.Add(project);
                }
            }
        }
        else
        {
            await base.LoadProjectsAsync();
        }
    }
}
