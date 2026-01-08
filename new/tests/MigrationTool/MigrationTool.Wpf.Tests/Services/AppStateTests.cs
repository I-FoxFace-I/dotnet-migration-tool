using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Wpf.Services;
using MigrationTool.Wpf.Tests.TestHelpers;
using Moq;
using Xunit;

namespace MigrationTool.Wpf.Tests.Services;

/// <summary>
/// Unit tests for AppState - shared application state.
/// </summary>
public class AppStateTests
{
    #region Test Constants

    private static class PropertyNames
    {
        public const string CurrentSolution = nameof(AppState.CurrentSolution);
        public const string HasSolution = nameof(AppState.HasSolution);
        public const string IsLoading = nameof(AppState.IsLoading);
        public const string WorkspacePath = nameof(AppState.WorkspacePath);
        public const string SolutionPath = nameof(AppState.SolutionPath);
        public const string ErrorMessage = nameof(AppState.ErrorMessage);
    }

    private static class ErrorMessages
    {
        public const string AnalyzerNotAvailable = "Solution analyzer not available";
        public const string FileNotFound = "File not found";
    }

    #endregion

    private readonly Mock<ISolutionAnalyzer> _analyzerMock;

    public AppStateTests()
    {
        _analyzerMock = new Mock<ISolutionAnalyzer>();
    }

    private AppState CreateAppState()
    {
        return new AppState(_analyzerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithAnalyzer_InitializesCorrectly()
    {
        // Act
        var appState = CreateAppState();

        // Assert
        appState.CurrentSolution.Should().BeNull();
        appState.HasSolution.Should().BeFalse();
        appState.IsLoading.Should().BeFalse();
        appState.WorkspacePath.Should().BeEmpty();
        appState.SolutionPath.Should().BeEmpty();
        appState.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithoutAnalyzer_InitializesCorrectly()
    {
        // Act
        var appState = new AppState();

        // Assert
        appState.HasSolution.Should().BeFalse();
    }

    #endregion

    #region LoadSolutionAsync Tests

    [Fact]
    public async Task LoadSolutionAsync_WithValidPath_LoadsSolution()
    {
        // Arrange
        var appState = CreateAppState();
        var solution = TestDataFactory.CreateDefaultSolution();
        var path = TestDataFactory.Paths.SolutionFile;

        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solution);

        // Act
        await appState.LoadSolutionAsync(path);

        // Assert
        appState.CurrentSolution.Should().Be(solution);
        appState.HasSolution.Should().BeTrue();
        appState.SolutionPath.Should().Be(path);
        appState.WorkspacePath.Should().Be(Path.GetDirectoryName(path));
        appState.ErrorMessage.Should().BeNull();
        appState.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSolutionAsync_WithoutAnalyzer_SetsErrorMessage()
    {
        // Arrange
        var appState = new AppState(); // No analyzer
        var path = TestDataFactory.Paths.SolutionFile;

        // Act
        await appState.LoadSolutionAsync(path);

        // Assert
        appState.CurrentSolution.Should().BeNull();
        appState.HasSolution.Should().BeFalse();
        appState.ErrorMessage.Should().Be(ErrorMessages.AnalyzerNotAvailable);
    }

    [Fact]
    public async Task LoadSolutionAsync_WhenAnalyzerThrows_SetsErrorMessage()
    {
        // Arrange
        var appState = CreateAppState();
        var path = TestDataFactory.Paths.SolutionFile;

        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException(ErrorMessages.FileNotFound));

        // Act
        await appState.LoadSolutionAsync(path);

        // Assert
        appState.CurrentSolution.Should().BeNull();
        appState.HasSolution.Should().BeFalse();
        appState.ErrorMessage.Should().Be(ErrorMessages.FileNotFound);
        appState.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSolutionAsync_SetsIsLoadingDuringOperation()
    {
        // Arrange
        var appState = CreateAppState();
        var path = TestDataFactory.Paths.SolutionFile;
        var tcs = new TaskCompletionSource<SolutionInfo>();
        var loadingStates = new List<bool>();

        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        appState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == PropertyNames.IsLoading)
            {
                loadingStates.Add(appState.IsLoading);
            }
        };

        // Act
        var loadTask = appState.LoadSolutionAsync(path);
        
        // Assert during loading
        appState.IsLoading.Should().BeTrue();
        
        // Complete loading
        tcs.SetResult(TestDataFactory.CreateDefaultSolution());
        await loadTask;

        // Assert after loading
        appState.IsLoading.Should().BeFalse();
        loadingStates.Should().Contain(true);
        loadingStates.Should().Contain(false);
    }

    #endregion

    #region RefreshSolutionAsync Tests

    [Fact]
    public async Task RefreshSolutionAsync_WithLoadedSolution_ReloadsFromSamePath()
    {
        // Arrange
        var appState = CreateAppState();
        var solution = TestDataFactory.CreateDefaultSolution();
        var path = TestDataFactory.Paths.SolutionFile;

        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solution);

        await appState.LoadSolutionAsync(path);

        // Act
        await appState.RefreshSolutionAsync();

        // Assert
        _analyzerMock.Verify(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RefreshSolutionAsync_WithoutLoadedSolution_DoesNothing()
    {
        // Arrange
        var appState = CreateAppState();

        // Act
        await appState.RefreshSolutionAsync();

        // Assert
        _analyzerMock.Verify(x => x.AnalyzeSolutionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ClearSolution Tests

    [Fact]
    public async Task ClearSolution_WithLoadedSolution_ClearsAllState()
    {
        // Arrange
        var appState = CreateAppState();
        var solution = TestDataFactory.CreateDefaultSolution();
        var path = TestDataFactory.Paths.SolutionFile;

        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solution);

        await appState.LoadSolutionAsync(path);

        // Act
        appState.ClearSolution();

        // Assert
        appState.CurrentSolution.Should().BeNull();
        appState.HasSolution.Should().BeFalse();
        appState.SolutionPath.Should().BeEmpty();
        appState.WorkspacePath.Should().BeEmpty();
        appState.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public async Task LoadSolutionAsync_RaisesPropertyChangedEvents()
    {
        // Arrange
        var appState = CreateAppState();
        var changedProperties = new List<string>();
        var path = TestDataFactory.Paths.SolutionFile;

        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateDefaultSolution());

        appState.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        await appState.LoadSolutionAsync(path);

        // Assert
        changedProperties.Should().Contain(PropertyNames.IsLoading);
        changedProperties.Should().Contain(PropertyNames.SolutionPath);
        changedProperties.Should().Contain(PropertyNames.WorkspacePath);
        changedProperties.Should().Contain(PropertyNames.CurrentSolution);
        changedProperties.Should().Contain(PropertyNames.HasSolution);
    }

    [Fact]
    public async Task ClearSolution_RaisesPropertyChangedEvents()
    {
        // Arrange - first load a solution so there's something to clear
        var appState = CreateAppState();
        var path = TestDataFactory.Paths.SolutionFile;
        
        _analyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateDefaultSolution());
        
        await appState.LoadSolutionAsync(path);
        
        var changedProperties = new List<string>();
        appState.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        appState.ClearSolution();

        // Assert
        changedProperties.Should().Contain(PropertyNames.CurrentSolution);
        changedProperties.Should().Contain(PropertyNames.SolutionPath);
        changedProperties.Should().Contain(PropertyNames.WorkspacePath);
    }

    #endregion
}
