using System.IO;
using System.Collections.ObjectModel;
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
/// Unit tests for SettingsViewModel - application settings and solution loading.
/// Uses TestableSettingsViewModel which inherits from real ViewModel.
/// </summary>
public class SettingsViewModelTests
{
    #region Test Constants

    private static class PropertyNames
    {
        public const string SolutionPath = "SolutionPath";
        public const string SelectedLanguageOption = "SelectedLanguageOption";
        public const string IsLoadingSolution = "IsLoadingSolution";
        public const string StatusMessage = "StatusMessage";
        public const string HasSolution = "HasSolution";
        public const string SolutionName = "SolutionName";
        public const string WorkspacePath = "WorkspacePath";
    }

    private static class TestPaths
    {
        public const string ValidSolutionPath = @"C:\test\solution.sln";
        public const string InvalidPath = "";
    }

    private static class Languages
    {
        public const string English = "en";
        public const string Czech = "cs";
        public const string Polish = "pl";
    }

    private static class StatusMessages
    {
        public const string Loading = "Loading...";
        public const string Success = "Success";
        public const string SelectWorkspace = "Select workspace";
    }

    #endregion

    private readonly Mock<ILogger<TestableSettingsViewModel>> _loggerMock;
    private readonly Mock<IAppState> _appStateMock;

    public SettingsViewModelTests()
    {
        _loggerMock = new Mock<ILogger<TestableSettingsViewModel>>();
        _appStateMock = new Mock<IAppState>();
    }

    private TestableSettingsViewModel CreateViewModel(SolutionInfo? initialSolution = null)
    {
        _appStateMock.Setup(x => x.CurrentSolution).Returns(initialSolution);
        _appStateMock.Setup(x => x.HasSolution).Returns(initialSolution != null);
        _appStateMock.Setup(x => x.IsLoading).Returns(false);
        _appStateMock.Setup(x => x.SolutionPath).Returns(initialSolution?.Path ?? string.Empty);
        _appStateMock.Setup(x => x.WorkspacePath).Returns(initialSolution != null ? Path.GetDirectoryName(initialSolution.Path) ?? "" : "");

        return new TestableSettingsViewModel(_loggerMock.Object, _appStateMock.Object);
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
    public void Constructor_InitializesLanguages()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Languages.Should().NotBeEmpty();
        viewModel.Languages.Should().Contain(l => l.Code == Languages.English);
    }

    [Fact]
    public void Constructor_SetsDefaultLanguage()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SelectedLanguageOption.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_InitializesWithEmptySolutionPath()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.SolutionPath.Should().BeEmpty();
        viewModel.HasSolution.Should().BeFalse();
    }

    #endregion

    #region BrowseSolution Tests

    [Fact]
    public void BrowseSolution_WhenPathSelected_SetsSolutionPath()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.SimulateBrowseSolution(TestPaths.ValidSolutionPath);

        // Assert
        viewModel.SolutionPath.Should().Be(TestPaths.ValidSolutionPath);
    }

    [Fact]
    public void BrowseSolution_WhenCancelled_DoesNotChangePath()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SolutionPath = TestPaths.ValidSolutionPath;

        // Act
        viewModel.SimulateBrowseSolution(null); // Simulates cancel

        // Assert
        viewModel.SolutionPath.Should().Be(TestPaths.ValidSolutionPath);
    }

    #endregion

    #region LoadSolution Tests

    [Fact]
    public async Task LoadSolutionAsync_WithEmptyPath_SetsStatusMessage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SolutionPath = TestPaths.InvalidPath;

        // Act
        await viewModel.TestLoadSolutionAsync();

        // Assert
        viewModel.StatusMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadSolutionAsync_WithValidPath_CallsAppStateLoadSolution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SolutionPath = TestPaths.ValidSolutionPath;

        // Act
        await viewModel.TestLoadSolutionAsync();

        // Assert
        _appStateMock.Verify(x => x.LoadSolutionAsync(TestPaths.ValidSolutionPath), Times.Once);
    }

    [Fact]
    public async Task LoadSolutionAsync_SetsLoadingStatus()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SolutionPath = TestPaths.ValidSolutionPath;
        
        // Setup mock to return a solution after loading
        _appStateMock.Setup(x => x.HasSolution).Returns(true);
        _appStateMock.Setup(x => x.CurrentSolution).Returns(TestDataFactory.CreateDefaultSolution());

        // Act
        await viewModel.TestLoadSolutionAsync();

        // Assert - Status should be set after loading (success message)
        viewModel.StatusMessage.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region ClearSolution Tests

    [Fact]
    public void ClearSolution_CallsAppStateClearSolution()
    {
        // Arrange
        var viewModel = CreateViewModel(TestDataFactory.CreateDefaultSolution());

        // Act
        viewModel.TestClearSolution();

        // Assert
        _appStateMock.Verify(x => x.ClearSolution(), Times.Once);
    }

    [Fact]
    public void ClearSolution_ClearsSolutionPath()
    {
        // Arrange
        var viewModel = CreateViewModel(TestDataFactory.CreateDefaultSolution());
        viewModel.SolutionPath = TestPaths.ValidSolutionPath;

        // Act
        viewModel.TestClearSolution();

        // Assert
        viewModel.SolutionPath.Should().BeEmpty();
    }

    #endregion

    #region RefreshSolution Tests

    [Fact]
    public async Task RefreshSolutionAsync_WithLoadedSolution_CallsAppStateRefresh()
    {
        // Arrange
        var viewModel = CreateViewModel(TestDataFactory.CreateDefaultSolution());

        // Act
        await viewModel.TestRefreshSolutionAsync();

        // Assert
        _appStateMock.Verify(x => x.RefreshSolutionAsync(), Times.Once);
    }

    [Fact]
    public async Task RefreshSolutionAsync_WithoutSolution_DoesNotCallRefresh()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.TestRefreshSolutionAsync();

        // Assert
        _appStateMock.Verify(x => x.RefreshSolutionAsync(), Times.Never);
    }

    #endregion

    #region Language Selection Tests

    [Fact]
    public void SelectedLanguageOption_WhenChanged_UpdatesLanguage()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var czechOption = viewModel.Languages.FirstOrDefault(l => l.Code == Languages.Czech);

        // Act
        if (czechOption != null)
        {
            viewModel.SelectedLanguageOption = czechOption;
        }

        // Assert
        viewModel.SelectedLanguageOption?.Code.Should().Be(Languages.Czech);
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void SolutionPath_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.SolutionPath = TestPaths.ValidSolutionPath;

        // Assert
        changedProperties.Should().Contain(PropertyNames.SolutionPath);
    }

    [Fact]
    public void StatusMessage_WhenChanged_RaisesPropertyChanged()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.StatusMessage = StatusMessages.Loading;

        // Assert
        changedProperties.Should().Contain(PropertyNames.StatusMessage);
    }

    #endregion
}

/// <summary>
/// Testable version of SettingsViewModel - inherits from real ViewModel and adds test helpers.
/// </summary>
public class TestableSettingsViewModel : SettingsViewModel
{
    public TestableSettingsViewModel(ILogger<TestableSettingsViewModel> logger, IAppState appState)
        : base(logger, appState)
    {
    }

    /// <summary>
    /// Simulates file dialog selection for testing.
    /// </summary>
    public void SimulateBrowseSolution(string? selectedPath)
    {
        if (!string.IsNullOrEmpty(selectedPath))
        {
            SolutionPath = selectedPath;
            StatusMessage = null;
        }
    }

    /// <summary>
    /// Exposes LoadSolutionAsync for testing (bypasses command).
    /// </summary>
    public Task TestLoadSolutionAsync() => LoadSolutionCommand.ExecuteAsync(null);

    /// <summary>
    /// Exposes ClearSolution for testing (bypasses command).
    /// </summary>
    public void TestClearSolution() => ClearSolutionCommand.Execute(null);

    /// <summary>
    /// Exposes RefreshSolutionAsync for testing (bypasses command).
    /// </summary>
    public Task TestRefreshSolutionAsync() => RefreshSolutionCommand.ExecuteAsync(null);
}
