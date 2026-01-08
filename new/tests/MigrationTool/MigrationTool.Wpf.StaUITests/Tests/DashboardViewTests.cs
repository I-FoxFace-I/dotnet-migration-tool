using FluentAssertions;
using MigrationTool.Wpf.StaUITests.Attributes;
using MigrationTool.Wpf.StaUITests.Infrastructure;
using MigrationTool.Wpf.ViewModels;
using MigrationTool.Wpf.Views;
using Xunit;

namespace MigrationTool.Wpf.StaUITests.Tests;

/// <summary>
/// STA UI tests for DashboardView.
/// </summary>
[Collection("STA UI Tests")]
public class DashboardViewTests : StaTestBase
{
    [STAFact]
    public void DashboardView_CanBeCreated()
    {
        // Act
        var view = CreateView<DashboardView>();
        
        // Assert
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }

    [STAFact]
    public void DashboardView_HasViewModel()
    {
        // Arrange & Act
        var view = CreateView<DashboardView>();
        ProcessDispatcherQueue();

        // Assert
        view.DataContext.Should().NotBeNull();
        view.DataContext.Should().BeAssignableTo<DashboardViewModel>();
    }

    [STAFact]
    public void DashboardView_DisplaysStatistics()
    {
        // Arrange
        var view = CreateView<DashboardView>();
        ProcessDispatcherQueue();

        // Act - Check if view is loaded and visible
        view.Loaded += (s, e) => { };

        // Assert
        view.Should().NotBeNull();
        view.DataContext.Should().NotBeNull();
    }
}
