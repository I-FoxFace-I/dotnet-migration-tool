using FluentAssertions;
using MigrationTool.Wpf.StaUITests.Attributes;
using MigrationTool.Wpf.StaUITests.Infrastructure;
using MigrationTool.Wpf.Views;
using Xunit;

namespace MigrationTool.Wpf.StaUITests.Tests;

/// <summary>
/// STA UI tests for all views - basic smoke tests.
/// </summary>
[Collection("STA UI Tests")]
public class AllViewsTests : StaTestBase
{
    [STAFact]
    public void DashboardView_LoadsSuccessfully()
    {
        // Act
        var view = CreateView<DashboardView>();
        ProcessDispatcherQueue();

        // Assert
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }

    [STAFact]
    public void ExplorerView_LoadsSuccessfully()
    {
        // Act
        var view = CreateView<ExplorerView>();
        ProcessDispatcherQueue();

        // Assert
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }

    [STAFact]
    public void PlannerView_LoadsSuccessfully()
    {
        // Act
        var view = CreateView<PlannerView>();
        ProcessDispatcherQueue();

        // Assert
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }

    [STAFact]
    public void SettingsView_LoadsSuccessfully()
    {
        // Act
        var view = CreateView<SettingsView>();
        ProcessDispatcherQueue();

        // Assert
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }
}
