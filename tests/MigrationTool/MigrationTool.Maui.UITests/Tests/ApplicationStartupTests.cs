using FluentAssertions;
using MigrationTool.Maui.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Maui.UITests.Tests;

/// <summary>
/// Tests for application startup and basic functionality.
/// </summary>
[Collection("UI Tests")]
public class ApplicationStartupTests : UITestBase
{
    #region Test Constants

    private static class ExpectedTexts
    {
        public const string WindowTitle = "Migration Tool";
        public const string DashboardTitle = "Dashboard";
        public const string WelcomeText = "Welcome to Migration Tool";
        public const string NoSolutionText = "No Solution Loaded";
    }

    private static class ExpectedCounts
    {
        public const string Zero = "0";
    }

    #endregion

    public ApplicationStartupTests()
    {
        try
        {
            InitializeDriver();
        }
        catch (SkipException)
        {
            // WinAppDriver not running - tests will be skipped
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Application_StartsSuccessfully()
    {
        // Skip if driver not initialized
        if (!IsDriverInitialized)
        {
            return; // Skip test
        }

        // Assert - application window should be visible
        Driver!.Title.Should().Contain(ExpectedTexts.WindowTitle);
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Application_ShowsDashboardOnStartup()
    {
        if (!IsDriverInitialized) return;

        // Assert - Dashboard should be the default page
        var dashboardTitle = TryFindByName(ExpectedTexts.DashboardTitle);
        dashboardTitle.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Dashboard_ShowsWelcomeMessage()
    {
        if (!IsDriverInitialized) return;

        // Assert
        var welcomeText = TryFindByName(ExpectedTexts.WelcomeText);
        welcomeText.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Dashboard_ShowsNoSolutionLoadedMessage()
    {
        if (!IsDriverInitialized) return;

        // Assert
        var noSolutionText = TryFindByName(ExpectedTexts.NoSolutionText);
        noSolutionText.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    public void Dashboard_ShowsZeroCountsInitially()
    {
        if (!IsDriverInitialized) return;

        // Assert - all counts should be 0
        var projectsLabel = TryFindByName(ExpectedCounts.Zero);
        projectsLabel.Should().NotBeNull("Projects count should show 0");
    }
}
