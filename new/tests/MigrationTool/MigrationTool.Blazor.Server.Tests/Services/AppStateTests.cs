using FluentAssertions;
using MigrationTool.Blazor.Server.Services;
using MigrationTool.Core.Abstractions.Models;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Services;

public class AppStateTests
{
    [Fact]
    public void CurrentSolution_Set_RaisesOnChange()
    {
        // Arrange
        var appState = new AppState();
        var solution = new SolutionInfo { Name = "TestSolution", Path = "test.sln" };
        var changeRaised = false;

        appState.OnChange += () => changeRaised = true;

        // Act
        appState.CurrentSolution = solution;

        // Assert
        appState.CurrentSolution.Should().Be(solution);
        appState.HasSolution.Should().BeTrue();
        changeRaised.Should().BeTrue();
    }

    [Fact]
    public void CurrentSolution_SetToNull_SetsHasSolutionToFalse()
    {
        // Arrange
        var appState = new AppState();
        appState.CurrentSolution = new SolutionInfo { Name = "Test", Path = "test.sln" };

        // Act
        appState.CurrentSolution = null;

        // Assert
        appState.CurrentSolution.Should().BeNull();
        appState.HasSolution.Should().BeFalse();
    }

    [Fact]
    public void WorkspacePath_Set_RaisesOnChange()
    {
        // Arrange
        var appState = new AppState();
        var changeRaised = false;

        appState.OnChange += () => changeRaised = true;

        // Act
        appState.WorkspacePath = "C:\\workspace";

        // Assert
        appState.WorkspacePath.Should().Be("C:\\workspace");
        changeRaised.Should().BeTrue();
    }

    [Fact]
    public void HasSolution_NoSolution_ReturnsFalse()
    {
        // Arrange
        var appState = new AppState();

        // Act & Assert
        appState.HasSolution.Should().BeFalse();
    }

    [Fact]
    public void HasSolution_WithSolution_ReturnsTrue()
    {
        // Arrange
        var appState = new AppState();
        var solution = new SolutionInfo { Name = "Test", Path = "test.sln" };

        // Act
        appState.CurrentSolution = solution;

        // Assert
        appState.HasSolution.Should().BeTrue();
    }
}
