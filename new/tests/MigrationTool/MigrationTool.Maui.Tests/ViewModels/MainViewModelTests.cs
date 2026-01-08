using FluentAssertions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization;
using Moq;
using Xunit;

namespace MigrationTool.Maui.Tests.ViewModels;

/// <summary>
/// Tests for MainViewModel - the main application state manager.
/// </summary>
public class MainViewModelTests
{
    private readonly Mock<ILocalizationService> _localizationMock;
    private readonly Mock<ISolutionAnalyzer> _solutionAnalyzerMock;

    public MainViewModelTests()
    {
        _localizationMock = new Mock<ILocalizationService>();
        _localizationMock.Setup(x => x.Get(It.IsAny<string>())).Returns((string key) => key);

        _solutionAnalyzerMock = new Mock<ISolutionAnalyzer>();
    }

    private TestableMainViewModel CreateViewModel()
    {
        return new TestableMainViewModel(_localizationMock.Object, _solutionAnalyzerMock.Object);
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
    public void Constructor_InitializesWithNoSolution()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.HasSolution.Should().BeFalse();
        viewModel.CurrentSolution.Should().BeNull();
        viewModel.WorkspacePath.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSolutionAsync_WithValidPath_LoadsSolution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solutionPath = @"C:\test\solution.sln";
        var expectedSolution = new SolutionInfo { Name = "TestSolution", Path = solutionPath };

        _solutionAnalyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSolution);

        // Act
        await viewModel.LoadSolutionCommand.ExecuteAsync(solutionPath);

        // Assert
        viewModel.CurrentSolution.Should().Be(expectedSolution);
        viewModel.HasSolution.Should().BeTrue();
        viewModel.WorkspacePath.Should().Be(@"C:\test");
    }

    [Fact]
    public async Task LoadSolutionAsync_WithEmptyPath_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadSolutionCommand.ExecuteAsync(string.Empty);

        // Assert
        viewModel.CurrentSolution.Should().BeNull();
        viewModel.HasSolution.Should().BeFalse();
        _solutionAnalyzerMock.Verify(x => x.AnalyzeSolutionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoadSolutionAsync_WithNullPath_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadSolutionCommand.ExecuteAsync(null!);

        // Assert
        viewModel.CurrentSolution.Should().BeNull();
        viewModel.HasSolution.Should().BeFalse();
    }

    [Fact]
    public async Task LoadSolutionAsync_SetsIsBusyDuringLoad()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solutionPath = @"C:\test\solution.sln";
        var busyStates = new List<bool>();

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsBusy))
                busyStates.Add(viewModel.IsBusy);
        };

        var tcs = new TaskCompletionSource<SolutionInfo>();
        _solutionAnalyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(solutionPath, It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        // Act
        var loadTask = viewModel.LoadSolutionCommand.ExecuteAsync(solutionPath);

        // Assert - should be busy during load
        viewModel.IsBusy.Should().BeTrue();

        // Complete the task
        tcs.SetResult(new SolutionInfo { Name = "Test", Path = solutionPath });
        await loadTask;

        // Assert - should not be busy after load
        viewModel.IsBusy.Should().BeFalse();
        busyStates.Should().Contain(true);
        busyStates.Should().EndWith(false);
    }

    [Fact]
    public async Task LoadSolutionAsync_WhenAnalyzerThrows_HandlesException()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solutionPath = @"C:\test\solution.sln";

        _solutionAnalyzerMock
            .Setup(x => x.AnalyzeSolutionAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("Solution not found"));

        // Act
        await viewModel.LoadSolutionCommand.ExecuteAsync(solutionPath);

        // Assert - should handle exception gracefully
        viewModel.CurrentSolution.Should().BeNull();
        viewModel.HasSolution.Should().BeFalse();
        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void ClearSolution_ClearsSolutionState()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SetCurrentSolution(new SolutionInfo { Name = "Test", Path = "path" });
        viewModel.SetHasSolution(true);

        // Act
        viewModel.ClearSolutionCommand.Execute(null);

        // Assert
        viewModel.CurrentSolution.Should().BeNull();
        viewModel.HasSolution.Should().BeFalse();
    }

    [Fact]
    public void ClearSolution_WhenNoSolution_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = () => viewModel.ClearSolutionCommand.Execute(null);
        act.Should().NotThrow();
    }
}

/// <summary>
/// Testable version of MainViewModel that exposes setters for testing.
/// This is a workaround since we can't directly reference the MAUI project.
/// </summary>
public class TestableMainViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly ISolutionAnalyzer _solutionAnalyzer;
    private readonly ILocalizationService _localization;

    private bool _isBusy;
    private string _title = string.Empty;
    private string _workspacePath = string.Empty;
    private SolutionInfo? _currentSolution;
    private bool _hasSolution;

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

    public string WorkspacePath
    {
        get => _workspacePath;
        set => SetProperty(ref _workspacePath, value);
    }

    public SolutionInfo? CurrentSolution
    {
        get => _currentSolution;
        set => SetProperty(ref _currentSolution, value);
    }

    public bool HasSolution
    {
        get => _hasSolution;
        set => SetProperty(ref _hasSolution, value);
    }

    public CommunityToolkit.Mvvm.Input.AsyncRelayCommand<string> LoadSolutionCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand ClearSolutionCommand { get; }

    public TestableMainViewModel(ILocalizationService localization, ISolutionAnalyzer solutionAnalyzer)
    {
        _localization = localization;
        _solutionAnalyzer = solutionAnalyzer;
        Title = _localization.Get("AppTitle");

        LoadSolutionCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<string>(LoadSolutionAsync);
        ClearSolutionCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ClearSolution);
    }

    private async Task LoadSolutionAsync(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return;

        try
        {
            IsBusy = true;
            CurrentSolution = await _solutionAnalyzer.AnalyzeSolutionAsync(solutionPath);
            HasSolution = CurrentSolution != null;
            WorkspacePath = Path.GetDirectoryName(solutionPath) ?? string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading solution: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearSolution()
    {
        CurrentSolution = null;
        HasSolution = false;
    }

    // Test helpers
    public void SetCurrentSolution(SolutionInfo? solution) => CurrentSolution = solution;
    public void SetHasSolution(bool value) => HasSolution = value;
}
