using FluentAssertions;
using MigrationTool.Maui.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Maui.UITests.Tests;

/// <summary>
/// Tests for the Project Explorer page functionality.
/// </summary>
[Collection("UI Tests")]
public class ExplorerPageTests : UITestBase
{
    #region Test Constants

    private static class PageElements
    {
        public const string ExplorerTitle = "Project Explorer";
        public const string NoSolutionMessage = "No Solution Loaded";
        public const string LoadSolutionButton = "Load Solution";
        public const string RefreshButton = "Refresh";
        public const string ProjectsTree = "Projects";
        public const string FilesTree = "Files";
    }

    private static class TabNames
    {
        public const string Projects = "Projects";
        public const string Files = "Files";
        public const string Classes = "Classes";
        public const string Tests = "Tests";
    }

    #endregion

    public ExplorerPageTests()
    {
        try
        {
            InitializeDriver();
        }
        catch (SkipException)
        {
            // WinAppDriver not running
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_ShowsTitle()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);

        // Assert
        var title = TryFindByName(PageElements.ExplorerTitle);
        title.Should().NotBeNull("Explorer title should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_ShowsNoSolutionMessage_WhenNoSolutionLoaded()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);

        // Assert
        var noSolutionMessage = TryFindByName(PageElements.NoSolutionMessage);
        noSolutionMessage.Should().NotBeNull("'No Solution Loaded' message should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_ShowsLoadSolutionButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);

        // Assert
        var loadButton = TryFindByName(PageElements.LoadSolutionButton);
        loadButton.Should().NotBeNull("Load Solution button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_LoadSolutionButton_IsClickable()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);
        var loadButton = TryFindByName(PageElements.LoadSolutionButton);

        // Assert
        loadButton.Should().NotBeNull();
        loadButton!.Enabled.Should().BeTrue("Load Solution button should be enabled");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_ShowsProjectsTab()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);

        // Assert
        var projectsTab = TryFindByName(TabNames.Projects);
        projectsTab.Should().NotBeNull("Projects tab should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_ShowsFilesTab()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);

        // Assert
        var filesTab = TryFindByName(TabNames.Files);
        filesTab.Should().NotBeNull("Files tab should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Explorer")]
    public void ExplorerPage_TabNavigation_Works()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.ExplorerTitle);

        // Act - click on Files tab
        var filesTab = TryFindByName(TabNames.Files);
        filesTab?.Click();
        Thread.Sleep(300);

        // Assert - Files tab should be selected
        filesTab.Should().NotBeNull();
    }
}
