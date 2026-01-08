using FluentAssertions;
using MigrationTool.Wpf.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Wpf.UITests.Tests;

/// <summary>
/// Tests for navigation between pages using the sidebar.
/// </summary>
[Collection("UI Tests")]
public class NavigationTests : UITestBase
{
    #region Test Constants

    private static class PageTitles
    {
        public const string Dashboard = "Dashboard";
        public const string Explorer = "Project Explorer";
        public const string Planner = "Migration Planner";
        public const string Settings = "Settings";
    }

    private static class MenuItems
    {
        public const string Dashboard = "Dashboard";
        public const string Explorer = "Explorer";
        public const string Planner = "Planner";
        public const string Settings = "Settings";
    }

    #endregion

    public NavigationTests()
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
    [Trait("Category", "Navigation")]
    public void Navigation_ToDashboard_ShowsDashboardPage()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateTo(MenuItems.Dashboard);

        // Assert
        var title = WaitForElement(PageTitles.Dashboard);
        title.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToExplorer_ShowsExplorerPage()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateTo(MenuItems.Explorer);

        // Assert
        var title = WaitForElement(PageTitles.Explorer);
        title.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToPlanner_ShowsPlannerPage()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateTo(MenuItems.Planner);

        // Assert
        var title = WaitForElement(PageTitles.Planner);
        title.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Navigation")]
    public void Navigation_ToSettings_ShowsSettingsPage()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateTo(MenuItems.Settings);

        // Assert
        var title = WaitForElement(PageTitles.Settings);
        title.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Navigation")]
    public void Navigation_BackToDashboard_FromSettings_Works()
    {
        if (!IsDriverInitialized) return;

        // Arrange - navigate to Settings first
        NavigateTo(MenuItems.Settings);
        WaitForElement(PageTitles.Settings);

        // Act - navigate back to Dashboard
        NavigateTo(MenuItems.Dashboard);

        // Assert
        var title = WaitForElement(PageTitles.Dashboard);
        title.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Navigation")]
    public void Navigation_AllPagesAccessible_InSequence()
    {
        if (!IsDriverInitialized) return;

        // Test navigating through all pages
        var pages = new[]
        {
            (MenuItems.Dashboard, PageTitles.Dashboard),
            (MenuItems.Explorer, PageTitles.Explorer),
            (MenuItems.Planner, PageTitles.Planner),
            (MenuItems.Settings, PageTitles.Settings),
            (MenuItems.Dashboard, PageTitles.Dashboard) // Back to start
        };

        foreach (var (menuItem, expectedTitle) in pages)
        {
            // Act
            NavigateTo(menuItem);

            // Assert
            var title = WaitForElement(expectedTitle);
            title.Should().NotBeNull($"Page '{expectedTitle}' should be accessible");
        }
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Navigation")]
    public void Navigation_RapidSwitching_DoesNotCrash()
    {
        if (!IsDriverInitialized) return;

        // Act - Rapidly switch between pages
        for (int i = 0; i < 5; i++)
        {
            NavigateTo(MenuItems.Dashboard);
            NavigateTo(MenuItems.Explorer);
            NavigateTo(MenuItems.Planner);
            NavigateTo(MenuItems.Settings);
        }

        // Assert - Application should still be responsive
        var title = WaitForElement(PageTitles.Settings);
        title.Should().NotBeNull("Application should remain responsive after rapid navigation");
    }
}
