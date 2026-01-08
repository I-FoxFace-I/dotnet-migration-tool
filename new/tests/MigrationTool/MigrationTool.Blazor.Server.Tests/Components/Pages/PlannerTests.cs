using Bunit;
using FluentAssertions;
using MigrationTool.Blazor.Server.Components.Pages;
using MigrationTool.Blazor.Server.Tests.TestHelpers;
using MigrationTool.Localization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Components.Pages;

public class PlannerTests : TestContext
{
    private readonly ILocalizationService _localization;

    public PlannerTests()
    {
        _localization = new LocalizationService();
        Services.AddSingleton<ILocalizationService>(_localization);
    }

    [Fact]
    public void Render_ShowsPlannerTitle()
    {
        // Act
        var cut = RenderComponent<Planner>();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.MigrationPlannerTitle);
    }

    [Fact]
    public void Render_ShowsActionButtons()
    {
        // Act
        var cut = RenderComponent<Planner>();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.CreatePlan);
        cut.Markup.Should().Contain(ExpectedTexts.LoadPlan);
        cut.Markup.Should().Contain(ExpectedTexts.SavePlan);
        cut.Markup.Should().Contain(ExpectedTexts.ExecutePlan);
    }

    [Fact]
    public void ClickNewPlan_CreatesNewPlan()
    {
        // Arrange
        var cut = RenderComponent<Planner>();

        // Act - find button by text content
        var buttons = cut.FindAll("button");
        var newPlanButton = buttons.FirstOrDefault(b => b.TextContent.Contains(ExpectedTexts.CreatePlan));
        newPlanButton?.Click();

        // Assert
        cut.Markup.Should().Contain(ExpectedTexts.PlanDetails);
    }

    [Fact]
    public void AddStep_AddsStepToTable()
    {
        // Arrange
        var cut = RenderComponent<Planner>();
        var buttons = cut.FindAll("button");
        var newPlanButton = buttons.FirstOrDefault(b => b.TextContent.Contains(ExpectedTexts.CreatePlan));
        newPlanButton?.Click();

        // Act
        var addButtons = cut.FindAll("button");
        var addStepButton = addButtons.FirstOrDefault(b => b.TextContent.Contains(ExpectedTexts.Add));
        addStepButton?.Click();

        // Assert
        cut.Markup.Should().Contain("steps-table");
    }
}
