using Bunit;
using FluentAssertions;
using MigrationTool.Blazor.Server.Components.Pages;
using MigrationTool.Blazor.Server.Services;
using MigrationTool.Blazor.Server.Tests.TestHelpers;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Localization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Components.Pages;

public class DashboardTests : TestContext
{
    private readonly AppState _appState;
    private readonly ILocalizationService _localization;

    public DashboardTests()
    {
        _appState = new AppState();
        _localization = new LocalizationService();

        Services.AddSingleton(_appState);
        Services.AddSingleton<ILocalizationService>(_localization);
    }

    [Fact]
    public void Render_NoSolution_ShowsEmptyState()
    {
        // Arrange
        _appState.CurrentSolution = null;

        // Act
        var cut = RenderComponent<Dashboard>();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.NoProjectsLoaded);
    }

    [Fact]
    public void Render_WithSolution_ShowsStats()
    {
        // Arrange
        var solution = new SolutionInfo
        {
            Name = "TestSolution",
            Path = "test.sln",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Project1",
                    Path = "p1.csproj",
                    IsTestProject = false,
                    SourceFiles = new List<SourceFileInfo>
                    {
                        new SourceFileInfo { Name = "File1.cs", Path = "f1.cs", Classes = new List<TypeInfo> { new TypeInfo { Name = "Class1" } } }
                    }
                },
                new ProjectInfo
                {
                    Name = "Project2",
                    Path = "p2.csproj",
                    IsTestProject = true,
                    SourceFiles = new List<SourceFileInfo>
                    {
                        new SourceFileInfo { Name = "Test1.cs", Path = "t1.cs", Classes = new List<TypeInfo> { new TypeInfo { Name = "TestClass1" } } }
                    }
                }
            }
        };

        _appState.CurrentSolution = solution;

        // Act
        var cut = RenderComponent<Dashboard>();

        // Assert
        cut.Markup.Should().Contain("TestSolution");
        cut.Markup.Should().Contain(ExpectedTexts.TotalProjects);
        cut.Markup.Should().Contain(ExpectedTexts.TestProjects);
    }

    [Fact]
    public void Render_WithSolution_DisplaysCorrectCounts()
    {
        // Arrange
        var solution = new SolutionInfo
        {
            Name = "TestSolution",
            Path = "test.sln",
            Projects = new List<ProjectInfo>
            {
                new ProjectInfo
                {
                    Name = "Project1",
                    Path = "p1.csproj",
                    IsTestProject = false,
                    SourceFiles = new List<SourceFileInfo>
                    {
                        new SourceFileInfo { Name = "File1.cs", Path = "f1.cs", Classes = new List<TypeInfo> { new TypeInfo { Name = "Class1" } } }
                    }
                }
            }
        };

        _appState.CurrentSolution = solution;

        // Act
        var cut = RenderComponent<Dashboard>();

        // Assert
        cut.Markup.Should().Contain("1"); // Project count
    }

    [Fact]
    public async Task AppStateChange_UpdatesComponent()
    {
        // Arrange
        var cut = RenderComponent<Dashboard>();
        cut.Markup.Should().Contain(ExpectedTexts.NoProjectsLoaded);

        var solution = new SolutionInfo
        {
            Name = "NewSolution",
            Path = "new.sln",
            Projects = new List<ProjectInfo>()
        };

        // Act
        await cut.InvokeAsync(() => _appState.CurrentSolution = solution);

        // Assert
        cut.Markup.Should().Contain("NewSolution");
    }
}
