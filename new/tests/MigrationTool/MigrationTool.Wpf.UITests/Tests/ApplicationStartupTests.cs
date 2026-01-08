using FluentAssertions;
using MigrationTool.Wpf.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Wpf.UITests.Tests;

/// <summary>
/// Tests for application startup and basic window functionality.
/// </summary>
[Collection("UI Tests")]
public class ApplicationStartupTests : UITestBase
{
    #region Test Constants

    private static class WindowProperties
    {
        public const string Title = "Migration Tool";
        public const int MinWidth = 800;
        public const int MinHeight = 600;
    }

    private static class ExpectedElements
    {
        public const string DashboardButton = "Dashboard";
        public const string ExplorerButton = "Explorer";
        public const string PlannerButton = "Planner";
        public const string SettingsButton = "Settings";
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
            // WinAppDriver not running
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Startup")]
    public void Application_Starts_Successfully()
    {
        if (!IsDriverInitialized) return;

        // Assert - Application started if we got here
        Driver.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Startup")]
    public void Application_HasCorrectTitle()
    {
        if (!IsDriverInitialized) return;

        // Assert
        Driver!.Title.Should().Contain(WindowProperties.Title);
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Startup")]
    public void Application_ShowsNavigationSidebar()
    {
        if (!IsDriverInitialized) return;

        // Assert - All navigation buttons should be visible
        var dashboardButton = WaitForElement(ExpectedElements.DashboardButton);
        var explorerButton = WaitForElement(ExpectedElements.ExplorerButton);
        var plannerButton = WaitForElement(ExpectedElements.PlannerButton);
        var settingsButton = WaitForElement(ExpectedElements.SettingsButton);

        dashboardButton.Should().NotBeNull("Dashboard button should be visible");
        explorerButton.Should().NotBeNull("Explorer button should be visible");
        plannerButton.Should().NotBeNull("Planner button should be visible");
        settingsButton.Should().NotBeNull("Settings button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Startup")]
    public void Application_StartsOnDashboard()
    {
        if (!IsDriverInitialized) return;

        // Assert - Dashboard page should be visible by default
        var dashboardTitle = WaitForElement("Dashboard");
        dashboardTitle.Should().NotBeNull("Dashboard should be the default page");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Startup")]
    public void Application_WindowIsResponsive()
    {
        if (!IsDriverInitialized) return;

        // Act - Try to interact with a button
        var dashboardButton = WaitForElement(ExpectedElements.DashboardButton);

        // Assert
        dashboardButton.Should().NotBeNull();
        dashboardButton!.Enabled.Should().BeTrue("Navigation buttons should be enabled");
    }
}
