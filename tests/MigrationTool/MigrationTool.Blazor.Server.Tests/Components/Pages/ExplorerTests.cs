using Bunit;
using FluentAssertions;
using MigrationTool.Blazor.Server.Components.Pages;
using MigrationTool.Blazor.Server.Services;
using MigrationTool.Blazor.Server.Tests.TestHelpers;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Components.Pages;

public class ExplorerTests : TestContext
{
    private readonly AppState _appState;
    private readonly Mock<IProjectAnalyzer> _mockProjectAnalyzer;
    private readonly ILocalizationService _localization;

    public ExplorerTests()
    {
        _appState = new AppState();
        _mockProjectAnalyzer = new Mock<IProjectAnalyzer>();
        _localization = new LocalizationService();

        Services.AddSingleton(_appState);
        Services.AddSingleton<IProjectAnalyzer>(_mockProjectAnalyzer.Object);
        Services.AddSingleton<ILocalizationService>(_localization);
    }

    [Fact]
    public void Render_NoSolution_ShowsEmptyState()
    {
        // Arrange
        _appState.CurrentSolution = null;

        // Act
        var cut = RenderComponent<Explorer>();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.NoProjectsLoaded);
    }

    [Fact]
    public void Render_WithSolution_LoadsProjects()
    {
        // Arrange
        var project = new ProjectInfo
        {
            Name = "TestProject",
            Path = "test.csproj",
            IsTestProject = false,
            SourceFiles = new List<SourceFileInfo>()
        };

        var solution = new SolutionInfo
        {
            Name = "TestSolution",
            Path = "test.sln",
            Projects = new List<ProjectInfo> { project }
        };

        _appState.CurrentSolution = solution;
        _mockProjectAnalyzer
            .Setup(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectInfo p, CancellationToken ct) => p);

        // Act
        var cut = RenderComponent<Explorer>();

        // Assert
        _mockProjectAnalyzer.Verify(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ClickProject_SelectsProject()
    {
        // Arrange
        var project = new ProjectInfo
        {
            Name = "TestProject",
            Path = "test.csproj",
            IsTestProject = false,
            SourceFiles = new List<SourceFileInfo>
            {
                new SourceFileInfo { Name = "File1.cs", Path = "f1.cs" }
            }
        };

        var solution = new SolutionInfo
        {
            Name = "TestSolution",
            Path = "test.sln",
            Projects = new List<ProjectInfo> { project }
        };

        _appState.CurrentSolution = solution;
        _mockProjectAnalyzer
            .Setup(x => x.EnrichProjectAsync(It.IsAny<ProjectInfo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectInfo p, CancellationToken ct) => p);

        var cut = RenderComponent<Explorer>();

        // Act
        var projectElement = cut.Find(".project-list li");
        projectElement.Click();

        // Assert
        cut.Markup.Should().Contain("TestProject");
    }
}
