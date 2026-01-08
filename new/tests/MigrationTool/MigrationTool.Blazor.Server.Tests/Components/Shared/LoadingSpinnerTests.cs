using Bunit;
using FluentAssertions;
using MigrationTool.Blazor.Server.Components.Shared;
using MigrationTool.Localization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Components.Shared;

public class LoadingSpinnerTests : TestContext
{
    public LoadingSpinnerTests()
    {
        Services.AddSingleton<ILocalizationService>(new LocalizationService());
    }

    [Fact]
    public void Render_ShowsSpinner()
    {
        // Act
        var cut = RenderComponent<LoadingSpinner>();

        // Assert
        cut.Markup.Should().Contain("spinner");
    }

    [Fact]
    public void Render_WithCustomMessage_ShowsMessage()
    {
        // Arrange
        var parameters = new ComponentParameter[]
        {
            ComponentParameter.CreateParameter(nameof(LoadingSpinner.Message), "Loading projects...")
        };

        // Act
        var cut = RenderComponent<LoadingSpinner>(parameters);

        // Assert
        cut.Markup.Should().Contain("Loading projects...");
    }

    [Fact]
    public void Render_WithoutMessage_ShowsDefaultLoading()
    {
        // Act
        var cut = RenderComponent<LoadingSpinner>();

        // Assert
        cut.Markup.Should().Contain("Loading");
    }
}
