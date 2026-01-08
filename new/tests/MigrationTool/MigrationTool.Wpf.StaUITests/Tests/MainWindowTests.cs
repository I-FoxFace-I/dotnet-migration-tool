using FluentAssertions;
using MigrationTool.Wpf.StaUITests.Attributes;
using MigrationTool.Wpf.StaUITests.Infrastructure;
using MigrationTool.Wpf.ViewModels;
using MigrationTool.Wpf.Views;
using Xunit;

namespace MigrationTool.Wpf.StaUITests.Tests;

/// <summary>
/// STA UI tests for MainWindow and navigation.
/// </summary>
[Collection("STA UI Tests")]
public class MainWindowTests : StaTestBase
{
    #region Test Constants

    private static class PageNames
    {
        public const string Dashboard = "Dashboard";
        public const string Explorer = "Explorer";
        public const string Planner = "Planner";
        public const string Settings = "Settings";
    }

    #endregion

    [STAFact]
    public void MainWindow_CanBeCreated()
    {
        // Act
        var window = CreateView<MainWindow>();
        
        // Assert
        window.Should().NotBeNull();
        window.DataContext.Should().NotBeNull();
    }

    [STAFact]
    public void MainWindow_HasMainViewModel()
    {
        // Arrange & Act
        var window = CreateView<MainWindow>();
        ProcessDispatcherQueue();

        // Assert
        window.DataContext.Should().NotBeNull();
        window.DataContext.Should().BeAssignableTo<MainViewModel>();
    }

    [STAFact]
    public void MainWindow_StartsWithDashboard()
    {
        // Arrange
        var window = CreateView<MainWindow>();
        var viewModel = (MainViewModel)window.DataContext;
        ProcessDispatcherQueue();

        // Assert
        viewModel.CurrentPage.Should().Be(PageNames.Dashboard);
    }

    [STAFact]
    public void MainWindow_Navigation_ToExplorer_Works()
    {
        // Arrange
        var window = CreateView<MainWindow>();
        var viewModel = (MainViewModel)window.DataContext;
        ProcessDispatcherQueue();

        // Act
        viewModel.CurrentPage = PageNames.Explorer;
        ProcessDispatcherQueue();

        // Assert
        viewModel.CurrentPage.Should().Be(PageNames.Explorer);
    }

    [STAFact]
    public void MainWindow_Navigation_ToPlanner_Works()
    {
        // Arrange
        var window = CreateView<MainWindow>();
        var viewModel = (MainViewModel)window.DataContext;
        ProcessDispatcherQueue();

        // Act
        viewModel.CurrentPage = PageNames.Planner;
        ProcessDispatcherQueue();

        // Assert
        viewModel.CurrentPage.Should().Be(PageNames.Planner);
    }

    [STAFact]
    public void MainWindow_Navigation_ToSettings_Works()
    {
        // Arrange
        var window = CreateView<MainWindow>();
        var viewModel = (MainViewModel)window.DataContext;
        ProcessDispatcherQueue();

        // Act
        viewModel.CurrentPage = PageNames.Settings;
        ProcessDispatcherQueue();

        // Assert
        viewModel.CurrentPage.Should().Be(PageNames.Settings);
    }

    [STAFact]
    public void MainWindow_Navigation_BackToDashboard_Works()
    {
        // Arrange
        var window = CreateView<MainWindow>();
        var viewModel = (MainViewModel)window.DataContext;
        ProcessDispatcherQueue();

        // Navigate away first
        viewModel.CurrentPage = PageNames.Settings;
        ProcessDispatcherQueue();

        // Act
        viewModel.CurrentPage = PageNames.Dashboard;
        ProcessDispatcherQueue();

        // Assert
        viewModel.CurrentPage.Should().Be(PageNames.Dashboard);
    }
}
