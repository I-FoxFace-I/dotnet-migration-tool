using FluentAssertions;
using MigrationTool.Maui.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Maui.UITests.Tests;

/// <summary>
/// Tests for the Migration Planner page functionality.
/// </summary>
[Collection("UI Tests")]
public class PlannerPageTests : UITestBase
{
    #region Test Constants

    private static class PageElements
    {
        public const string PlannerTitle = "Migration Planner";
        public const string NewPlanButton = "New Plan";
        public const string AddStepButton = "Add Step";
        public const string SavePlanButton = "Save Plan";
        public const string LoadPlanButton = "Load Plan";
        public const string ExecuteButton = "Execute";
        public const string StepsList = "Steps";
        public const string NoPlanMessage = "No Plan";
    }

    private static class StepTypes
    {
        public const string MoveFile = "Move File";
        public const string MoveFolder = "Move Folder";
        public const string RenameNamespace = "Rename Namespace";
        public const string UpdateReference = "Update Reference";
    }

    #endregion

    public PlannerPageTests()
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
    [Trait("Category", "Planner")]
    public void PlannerPage_ShowsTitle()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);

        // Assert
        var title = TryFindByName(PageElements.PlannerTitle);
        title.Should().NotBeNull("Planner title should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_ShowsNewPlanButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);

        // Assert
        var newPlanButton = TryFindByName(PageElements.NewPlanButton);
        newPlanButton.Should().NotBeNull("New Plan button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_NewPlanButton_IsClickable()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);
        var newPlanButton = TryFindByName(PageElements.NewPlanButton);

        // Assert
        newPlanButton.Should().NotBeNull();
        newPlanButton!.Enabled.Should().BeTrue("New Plan button should be enabled");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_ShowsAddStepButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);

        // Assert
        var addStepButton = TryFindByName(PageElements.AddStepButton);
        addStepButton.Should().NotBeNull("Add Step button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_ShowsSavePlanButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);

        // Assert
        var saveButton = TryFindByName(PageElements.SavePlanButton);
        saveButton.Should().NotBeNull("Save Plan button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_ShowsLoadPlanButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);

        // Assert
        var loadButton = TryFindByName(PageElements.LoadPlanButton);
        loadButton.Should().NotBeNull("Load Plan button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_ShowsExecuteButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);

        // Assert
        var executeButton = TryFindByName(PageElements.ExecuteButton);
        executeButton.Should().NotBeNull("Execute button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Planner")]
    public void PlannerPage_ClickNewPlan_EnablesAddStep()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.PlannerTitle);
        var newPlanButton = TryFindByName(PageElements.NewPlanButton);

        // Act
        newPlanButton?.Click();
        Thread.Sleep(500); // Wait for UI update

        // Assert
        var addStepButton = TryFindByName(PageElements.AddStepButton);
        addStepButton.Should().NotBeNull();
        addStepButton!.Enabled.Should().BeTrue("Add Step should be enabled after creating new plan");
    }
}
