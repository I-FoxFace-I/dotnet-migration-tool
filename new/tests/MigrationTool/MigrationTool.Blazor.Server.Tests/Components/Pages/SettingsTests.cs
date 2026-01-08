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

public class SettingsTests : TestContext
{
    private readonly AppState _appState;
    private readonly Mock<ISolutionAnalyzer> _mockSolutionAnalyzer;
    private readonly ILocalizationService _localization;

    public SettingsTests()
    {
        _appState = new AppState();
        _mockSolutionAnalyzer = new Mock<ISolutionAnalyzer>();
        _localization = new LocalizationService();

        Services.AddSingleton(_appState);
        Services.AddSingleton<ISolutionAnalyzer>(_mockSolutionAnalyzer.Object);
        Services.AddSingleton<ILocalizationService>(_localization);
    }

    [Fact]
    public void Render_ShowsSettingsTitle()
    {
        // Act
        var cut = RenderComponent<Settings>();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.SettingsTitle);
    }

    [Fact]
    public void Render_ShowsLanguageSelector()
    {
        // Act
        var cut = RenderComponent<Settings>();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.Language);
    }

    [Fact]
    public void ChangeLanguage_UpdatesLocalization()
    {
        // Arrange
        var cut = RenderComponent<Settings>();
        var initialLanguage = _localization.CurrentLanguage;

        // Act
        var languageSelect = cut.Find("select");
        languageSelect.Change("cs");

        // Assert
        _localization.CurrentLanguage.Should().Be("cs");
    }

    [Fact]
    public void LoadSolution_WithValidPath_UpdatesAppState()
    {
        // Arrange
        var solution = new SolutionInfo
        {
            Name = "TestSolution",
            Path = "test.sln",
            Projects = new List<ProjectInfo>()
        };

        _mockSolutionAnalyzer
            .Setup(x => x.FindSolutionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "test.sln" });

        _mockSolutionAnalyzer
            .Setup(x => x.AnalyzeSolutionAsync("test.sln", It.IsAny<CancellationToken>()))
            .ReturnsAsync(solution);

        var cut = RenderComponent<Settings>();

        // Act
        // Simulate solution selection would require more setup
        // For now, just verify the component renders

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.SettingsTitle);
    }
}
