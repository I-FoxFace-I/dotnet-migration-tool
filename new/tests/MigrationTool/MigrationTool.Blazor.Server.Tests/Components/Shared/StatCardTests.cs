using Bunit;
using FluentAssertions;
using MigrationTool.Blazor.Server.Components.Shared;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Components.Shared;

public class StatCardTests : TestContext
{
    [Fact]
    public void Render_DisplaysIconAndValue()
    {
        // Arrange
        var parameters = new ComponentParameter[]
        {
            ComponentParameter.CreateParameter(nameof(StatCard.Icon), "📊"),
            ComponentParameter.CreateParameter(nameof(StatCard.Label), "Total Projects"),
            ComponentParameter.CreateParameter(nameof(StatCard.Value), 42),
            ComponentParameter.CreateParameter(nameof(StatCard.ColorClass), "primary")
        };

        // Act
        var cut = RenderComponent<StatCard>(parameters);

        // Assert
        cut.Markup.Should().Contain("📊");
        cut.Markup.Should().Contain("42");
        cut.Markup.Should().Contain("Total Projects");
    }

    [Fact]
    public void Render_WithColorClass_AppliesClass()
    {
        // Arrange
        var parameters = new ComponentParameter[]
        {
            ComponentParameter.CreateParameter(nameof(StatCard.Icon), "📊"),
            ComponentParameter.CreateParameter(nameof(StatCard.Label), "Test"),
            ComponentParameter.CreateParameter(nameof(StatCard.Value), 10),
            ComponentParameter.CreateParameter(nameof(StatCard.ColorClass), "success")
        };

        // Act
        var cut = RenderComponent<StatCard>(parameters);

        // Assert
        cut.Markup.Should().Contain("stat-card success");
    }
}
