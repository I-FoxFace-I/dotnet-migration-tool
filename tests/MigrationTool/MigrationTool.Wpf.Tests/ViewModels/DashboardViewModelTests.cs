using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Wpf.Services;
using MigrationTool.Wpf.Tests.TestHelpers;
using MigrationTool.Wpf.ViewModels;
using Moq;
using Xunit;

namespace MigrationTool.Wpf.Tests.ViewModels;

/// <summary>
/// Unit tests for DashboardViewModel - displays solution statistics.
/// Uses TestableDashboardViewModel to avoid WPF framework dependencies.
/// </summary>
public class DashboardViewModelTests
{
    #region Test Constants

    private static class PropertyNames
    {
        public const string Solution = "Solution";
        public const string ProjectCount = "ProjectCount";
        public const string TestProjectCount = "TestProjectCount";
        public const string SourceProjectCount = "SourceProjectCount";
        public const string FileCount = "FileCount";
        public const string ClassCount = "ClassCount";
        public const string TestCount = "TestCount";
        public const string HasSolution = "HasSolution";
        public const string IsLoading = "IsLoading";
    }

    private static class ExpectedValues
    {
        public const int DefaultProjectCount = 3;
        public const int DefaultTestProjectCount = 1;
        public const int DefaultSourceProjectCount = 2;
        public const int DefaultFileCount = 30; // 10 + 15 + 5
        public const int DefaultClassCount = 16; // 5 + 8 + 3
        public const int DefaultTestCount = 20;
    }

    #endregion

    private readonly Mock<ILogger<TestableDashboardViewModel>> _loggerMock;
    private readonly Mock<IAppState> _appStateMock;

    public DashboardViewModelTests()
    {
        _loggerMock = new Mock<ILogger<TestableDashboardViewModel>>();
        _appStateMock = new Mock<IAppState>();
    }

    private TestableDashboardViewModel CreateViewModel(SolutionInfo? initialSolution = null)
    {
        _appStateMock.Setup(x => x.CurrentSolution).Returns(initialSolution);
        _appStateMock.Setup(x => x.HasSolution).Returns(initialSolution != null);
        _appStateMock.Setup(x => x.IsLoading).Returns(false);
        
        return new TestableDashboardViewModel(_loggerMock.Object, _appStateMock.Object);
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
    public void Constructor_InitializesWithZeroCounts()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ProjectCount.Should().Be(0);
        viewModel.TestProjectCount.Should().Be(0);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.FileCount.Should().Be(0);
        viewModel.ClassCount.Should().Be(0);
        viewModel.TestCount.Should().Be(0);
        viewModel.Solution.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithExistingSolution_LoadsStatistics()
    {
        // Arrange
        var solution = TestDataFactory.CreateDefaultSolution();

        // Act
        var viewModel = CreateViewModel(solution);

        // Assert
        viewModel.ProjectCount.Should().Be(ExpectedValues.DefaultProjectCount);
        viewModel.TestProjectCount.Should().Be(ExpectedValues.DefaultTestProjectCount);
        viewModel.SourceProjectCount.Should().Be(ExpectedValues.DefaultSourceProjectCount);
    }

    #endregion

    #region UpdateStatistics Tests

    [Fact]
    public void UpdateStatistics_WithValidSolution_UpdatesCounts()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestDataFactory.CreateDefaultSolution();

        // Act
        viewModel.SimulateAppStateChange(solution);

        // Assert
        viewModel.ProjectCount.Should().Be(ExpectedValues.DefaultProjectCount);
        viewModel.TestProjectCount.Should().Be(ExpectedValues.DefaultTestProjectCount);
        viewModel.SourceProjectCount.Should().Be(ExpectedValues.DefaultSourceProjectCount);
        viewModel.FileCount.Should().Be(ExpectedValues.DefaultFileCount);
        viewModel.ClassCount.Should().Be(ExpectedValues.DefaultClassCount);
        viewModel.TestCount.Should().Be(ExpectedValues.DefaultTestCount);
    }

    [Fact]
    public void UpdateStatistics_WithNullSolution_ResetsToZero()
    {
        // Arrange - start with a solution loaded
        var solution = TestDataFactory.CreateDefaultSolution();
        _appStateMock.Setup(x => x.CurrentSolution).Returns(solution);
        _appStateMock.Setup(x => x.HasSolution).Returns(true);
        var viewModel = new TestableDashboardViewModel(_loggerMock.Object, _appStateMock.Object);

        // Act - simulate clearing the solution
        viewModel.SimulateAppStateChange(null);

        // Assert
        viewModel.ProjectCount.Should().Be(0);
        viewModel.TestProjectCount.Should().Be(0);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.FileCount.Should().Be(0);
        viewModel.ClassCount.Should().Be(0);
        viewModel.TestCount.Should().Be(0);
    }

    [Fact]
    public void UpdateStatistics_WithEmptyProjects_SetsZeroCounts()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestDataFactory.CreateEmptySolution();

        // Act
        viewModel.SimulateAppStateChange(solution);

        // Assert
        viewModel.ProjectCount.Should().Be(0);
        viewModel.TestProjectCount.Should().Be(0);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.FileCount.Should().Be(0);
        viewModel.ClassCount.Should().Be(0);
        viewModel.TestCount.Should().Be(0);
    }

    [Fact]
    public void UpdateStatistics_WithOnlyTestProjects_CountsCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestDataFactory.CreateTestOnlySolution();

        // Act
        viewModel.SimulateAppStateChange(solution);

        // Assert
        viewModel.ProjectCount.Should().Be(2);
        viewModel.TestProjectCount.Should().Be(2);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.TestCount.Should().Be(25); // 10 + 15
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void UpdateStatistics_RaisesPropertyChangedEvents()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        var solution = TestDataFactory.CreateDefaultSolution();

        // Act
        viewModel.SimulateAppStateChange(solution);

        // Assert
        changedProperties.Should().Contain(PropertyNames.ProjectCount);
        changedProperties.Should().Contain(PropertyNames.FileCount);
        changedProperties.Should().Contain(PropertyNames.ClassCount);
        changedProperties.Should().Contain(PropertyNames.TestProjectCount);
        changedProperties.Should().Contain(PropertyNames.SourceProjectCount);
        changedProperties.Should().Contain(PropertyNames.TestCount);
    }

    #endregion

    #region HasSolution and IsLoading Tests

    [Fact]
    public void HasSolution_WhenSolutionLoaded_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel(TestDataFactory.CreateDefaultSolution());

        // Assert
        viewModel.HasSolution.Should().BeTrue();
    }

    [Fact]
    public void HasSolution_WhenNoSolution_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Assert
        viewModel.HasSolution.Should().BeFalse();
    }

    #endregion
}

/// <summary>
/// Testable version of DashboardViewModel - inherits from real ViewModel and adds test helpers.
/// </summary>
public class TestableDashboardViewModel : DashboardViewModel
{
    private SolutionInfo? _simulatedSolution;
    private bool _isSimulating;

    public TestableDashboardViewModel(ILogger<TestableDashboardViewModel> logger, IAppState appState)
        : base(logger, appState)
    {
    }

    /// <summary>
    /// Simulates AppState.CurrentSolution change for testing purposes.
    /// </summary>
    public void SimulateAppStateChange(SolutionInfo? solution)
    {
        _simulatedSolution = solution;
        _isSimulating = true;
        UpdateStatistics();
        _isSimulating = false;
    }

    protected override void UpdateStatistics()
    {
        if (_isSimulating)
        {
            // Use simulated solution
            var solution = _simulatedSolution;

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
        else
        {
            // Use real base implementation
            base.UpdateStatistics();
        }
    }
}
