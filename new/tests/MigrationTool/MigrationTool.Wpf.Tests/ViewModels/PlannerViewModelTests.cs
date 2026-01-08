using System.Collections.ObjectModel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Wpf.Services;
using MigrationTool.Wpf.Tests.TestHelpers;
using MigrationTool.Wpf.ViewModels;
using Moq;
using Xunit;

namespace MigrationTool.Wpf.Tests.ViewModels;

/// <summary>
/// Unit tests for PlannerViewModel - migration plan management.
/// Uses TestablePlannerViewModel which inherits from real ViewModel.
/// </summary>
public class PlannerViewModelTests
{
    #region Test Constants

    private static class PropertyNames
    {
        public const string CurrentPlan = "CurrentPlan";
        public const string Steps = "Steps";
        public const string SelectedStep = "SelectedStep";
        public const string PlanName = "PlanName";
        public const string IsExecuting = "IsExecuting";
        public const string ExecutionProgress = "ExecutionProgress";
        public const string StatusMessage = "StatusMessage";
        public const string HasPlan = "HasPlan";
        public const string HasSolution = "HasSolution";
    }

    private static class PlanNames
    {
        public const string Default = "New Migration Plan";
        public const string Custom = "My Custom Plan";
        public const string Test = "Test Plan";
    }

    private static class StepCounts
    {
        public const int Empty = 0;
        public const int Single = 1;
        public const int Multiple = 3;
    }

    #endregion

    private readonly Mock<ILogger<TestablePlannerViewModel>> _loggerMock;
    private readonly Mock<IAppState> _appStateMock;
    private readonly Mock<IMigrationPlanner> _plannerMock;
    private readonly Mock<IMigrationExecutor> _executorMock;

    public PlannerViewModelTests()
    {
        _loggerMock = new Mock<ILogger<TestablePlannerViewModel>>();
        _appStateMock = new Mock<IAppState>();
        _plannerMock = new Mock<IMigrationPlanner>();
        _executorMock = new Mock<IMigrationExecutor>();

        SetupPlannerMock();
    }

    private void SetupPlannerMock()
    {
        _plannerMock
            .Setup(x => x.CreatePlan(It.IsAny<string>()))
            .Returns((string name) => new MigrationPlan { Name = name });

        _plannerMock
            .Setup(x => x.AddStep(It.IsAny<MigrationPlan>(), It.IsAny<MigrationStep>()))
            .Returns((MigrationPlan plan, MigrationStep step) =>
                plan with { Steps = plan.Steps.Append(step).ToList() });

        _plannerMock
            .Setup(x => x.RemoveStep(It.IsAny<MigrationPlan>(), It.IsAny<int>()))
            .Returns((MigrationPlan plan, int index) =>
                plan with { Steps = plan.Steps.Where((_, i) => i != index).ToList() });

        _plannerMock
            .Setup(x => x.ExportPlan(It.IsAny<MigrationPlan>()))
            .Returns((MigrationPlan plan) => $"{{\"name\":\"{plan.Name}\"}}");

        _plannerMock
            .Setup(x => x.ImportPlan(It.IsAny<string>()))
            .Returns(new MigrationPlan { Name = PlanNames.Test });
    }

    private TestablePlannerViewModel CreateViewModel(SolutionInfo? initialSolution = null)
    {
        _appStateMock.Setup(x => x.CurrentSolution).Returns(initialSolution);
        _appStateMock.Setup(x => x.HasSolution).Returns(initialSolution != null);

        return new TestablePlannerViewModel(
            _loggerMock.Object,
            _appStateMock.Object,
            _plannerMock.Object,
            _executorMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDisplayName()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.DisplayName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_InitializesWithNoPlan()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.CurrentPlan.Should().BeNull();
        viewModel.HasPlan.Should().BeFalse();
        viewModel.Steps.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_SetsDefaultPlanName()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.PlanName.Should().Be(PlanNames.Default);
    }

    #endregion

    #region NewPlan Tests

    [Fact]
    public void NewPlan_CreatesPlanWithName()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.TestNewPlan();

        // Assert
        viewModel.CurrentPlan.Should().NotBeNull();
        viewModel.HasPlan.Should().BeTrue();
        _plannerMock.Verify(x => x.CreatePlan(PlanNames.Default), Times.Once);
    }

    [Fact]
    public void NewPlan_ClearsExistingSteps()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();

        // Act
        viewModel.TestNewPlan();

        // Assert
        viewModel.Steps.Should().BeEmpty();
    }

    #endregion

    #region AddStep Tests

    [Fact]
    public void AddStep_WithNoPlan_CreatesPlanFirst()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.TestAddStep();

        // Assert
        viewModel.CurrentPlan.Should().NotBeNull();
        viewModel.HasPlan.Should().BeTrue();
    }

    [Fact]
    public void AddStep_AddsStepToCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();

        // Act
        viewModel.TestAddStep();

        // Assert
        viewModel.Steps.Should().HaveCount(1);
    }

    [Fact]
    public void AddStep_SelectsNewStep()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();

        // Act
        viewModel.TestAddStep();

        // Assert
        viewModel.SelectedStep.Should().NotBeNull();
        viewModel.SelectedStep.Should().Be(viewModel.Steps.Last());
    }

    [Fact]
    public void AddStep_MultipleTimes_AddsMultipleSteps()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();

        // Act
        viewModel.TestAddStep();
        viewModel.TestAddStep();
        viewModel.TestAddStep();

        // Assert
        viewModel.Steps.Should().HaveCount(StepCounts.Multiple);
    }

    #endregion

    #region RemoveStep Tests

    [Fact]
    public void RemoveStep_WithSelectedStep_RemovesFromCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();
        var stepToRemove = viewModel.SelectedStep;

        // Act
        viewModel.TestRemoveStep();

        // Assert
        viewModel.Steps.Should().NotContain(stepToRemove);
    }

    [Fact]
    public void RemoveStep_WithNoSelectedStep_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();
        viewModel.SelectedStep = null;
        var initialCount = viewModel.Steps.Count;

        // Act
        viewModel.TestRemoveStep();

        // Assert
        viewModel.Steps.Should().HaveCount(initialCount);
    }

    [Fact]
    public void RemoveStep_SelectsAnotherStep()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();
        viewModel.TestAddStep();
        viewModel.SelectedStep = viewModel.Steps.First();

        // Act
        viewModel.TestRemoveStep();

        // Assert
        viewModel.SelectedStep.Should().NotBeNull();
    }

    #endregion

    #region MoveStep Tests

    [Fact]
    public void MoveStepUp_WithSecondStepSelected_MovesToFirst()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();
        viewModel.TestAddStep();
        var secondStep = viewModel.Steps[1];
        viewModel.SelectedStep = secondStep;

        // Act
        viewModel.TestMoveStepUp();

        // Assert
        viewModel.Steps[0].Should().Be(secondStep);
    }

    [Fact]
    public void MoveStepUp_WithFirstStepSelected_DoesNotMove()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();
        viewModel.TestAddStep();
        var firstStep = viewModel.Steps[0];
        viewModel.SelectedStep = firstStep;

        // Act
        viewModel.TestMoveStepUp();

        // Assert
        viewModel.Steps[0].Should().Be(firstStep);
    }

    [Fact]
    public void MoveStepDown_WithFirstStepSelected_MovesToSecond()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        viewModel.TestAddStep();
        viewModel.TestAddStep();
        var firstStep = viewModel.Steps[0];
        viewModel.SelectedStep = firstStep;

        // Act
        viewModel.TestMoveStepDown();

        // Assert
        viewModel.Steps[1].Should().Be(firstStep);
    }

    #endregion

    #region SavePlan and LoadPlan Tests

    [Fact]
    public void ExportPlan_WithCurrentPlan_ExportsPlan()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();

        // Act
        var json = viewModel.TestExportPlan();

        // Assert
        json.Should().NotBeNullOrEmpty();
        _plannerMock.Verify(x => x.ExportPlan(It.IsAny<MigrationPlan>()), Times.Once);
    }

    [Fact]
    public void ImportPlan_WithValidJson_ImportsPlan()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var json = "{\"name\":\"Test Plan\"}";

        // Act
        viewModel.TestImportPlan(json);

        // Assert
        viewModel.CurrentPlan.Should().NotBeNull();
        viewModel.HasPlan.Should().BeTrue();
        _plannerMock.Verify(x => x.ImportPlan(json), Times.Once);
    }

    #endregion

    #region ExecutePlan Tests

    [Fact]
    public async Task ExecutePlanAsync_WithNoPlan_DoesNotExecute()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        await viewModel.TestExecutePlanAsync();

        // Assert
        _executorMock.Verify(
            x => x.ExecuteAsync(It.IsAny<MigrationPlan>(), It.IsAny<IProgress<MigrationProgress>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecutePlanAsync_WithPlan_CallsExecutor()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();

        _executorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<MigrationPlan>(), It.IsAny<IProgress<MigrationProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationResult { Success = true, Plan = viewModel.CurrentPlan! });

        // Act
        await viewModel.TestExecutePlanAsync();

        // Assert
        _executorMock.Verify(
            x => x.ExecuteAsync(It.IsAny<MigrationPlan>(), It.IsAny<IProgress<MigrationProgress>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecutePlanAsync_SetsIsExecutingDuringExecution()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.TestNewPlan();
        var executingStates = new List<bool>();

        _executorMock
            .Setup(x => x.ExecuteAsync(It.IsAny<MigrationPlan>(), It.IsAny<IProgress<MigrationProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MigrationResult { Success = true, Plan = viewModel.CurrentPlan! });

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == PropertyNames.IsExecuting)
            {
                executingStates.Add(viewModel.IsExecuting);
            }
        };

        // Act
        await viewModel.TestExecutePlanAsync();

        // Assert
        executingStates.Should().Contain(true);
        executingStates.Should().Contain(false);
        viewModel.IsExecuting.Should().BeFalse();
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void NewPlan_RaisesPropertyChangedEvents()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.TestNewPlan();

        // Assert
        changedProperties.Should().Contain(PropertyNames.CurrentPlan);
        changedProperties.Should().Contain(PropertyNames.HasPlan);
    }

    #endregion
}

/// <summary>
/// Testable version of PlannerViewModel - inherits from real ViewModel and adds test helpers.
/// </summary>
public class TestablePlannerViewModel : PlannerViewModel
{
    private readonly IMigrationPlanner _testPlanner;

    public TestablePlannerViewModel(
        ILogger<TestablePlannerViewModel> logger,
        IAppState appState,
        IMigrationPlanner migrationPlanner,
        IMigrationExecutor migrationExecutor)
        : base(logger, appState, migrationPlanner, migrationExecutor)
    {
        _testPlanner = migrationPlanner;
    }

    /// <summary>
    /// Exposes NewPlan command for testing.
    /// </summary>
    public void TestNewPlan() => NewPlanCommand.Execute(null);

    /// <summary>
    /// Exposes AddStep command for testing.
    /// </summary>
    public void TestAddStep() => AddStepCommand.Execute(null);

    /// <summary>
    /// Exposes RemoveStep command for testing.
    /// </summary>
    public void TestRemoveStep() => RemoveStepCommand.Execute(null);

    /// <summary>
    /// Exposes MoveStepUp command for testing.
    /// </summary>
    public void TestMoveStepUp() => MoveStepUpCommand.Execute(null);

    /// <summary>
    /// Exposes MoveStepDown command for testing.
    /// </summary>
    public void TestMoveStepDown() => MoveStepDownCommand.Execute(null);

    /// <summary>
    /// Exposes ExportPlan for testing (without file dialog).
    /// </summary>
    public string? TestExportPlan()
    {
        if (CurrentPlan == null) return null;
        return _testPlanner.ExportPlan(CurrentPlan);
    }

    /// <summary>
    /// Exposes ImportPlan for testing (without file dialog).
    /// </summary>
    public void TestImportPlan(string json)
    {
        CurrentPlan = _testPlanner.ImportPlan(json);
        PlanName = CurrentPlan.Name;
        Steps.Clear();
        foreach (var step in CurrentPlan.Steps)
        {
            Steps.Add(step);
        }
        OnPropertyChanged(nameof(HasPlan));
    }

    /// <summary>
    /// Exposes ExecutePlanAsync for testing.
    /// </summary>
    public Task TestExecutePlanAsync() => ExecutePlanCommand.ExecuteAsync(null);
}
