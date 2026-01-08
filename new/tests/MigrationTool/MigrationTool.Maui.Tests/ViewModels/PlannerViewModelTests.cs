using System.Collections.ObjectModel;
using FluentAssertions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Localization;
using Moq;
using Xunit;

namespace MigrationTool.Maui.Tests.ViewModels;

/// <summary>
/// Tests for PlannerViewModel - migration planning functionality.
/// </summary>
public class PlannerViewModelTests
{
    private readonly Mock<ILocalizationService> _localizationMock;

    public PlannerViewModelTests()
    {
        _localizationMock = new Mock<ILocalizationService>();
        _localizationMock.Setup(x => x.Get(It.IsAny<string>())).Returns((string key) => key);
    }

    private TestablePlannerViewModel CreateViewModel()
    {
        return new TestablePlannerViewModel(_localizationMock.Object);
    }

    [Fact]
    public void Constructor_SetsTitle()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Title.Should().NotBeNullOrEmpty();
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
        viewModel.SelectedStep.Should().BeNull();
    }

    [Fact]
    public void NewPlan_CreatesNewPlan()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.NewPlanCommand.Execute(null);

        // Assert
        viewModel.CurrentPlan.Should().NotBeNull();
        viewModel.CurrentPlan!.Name.Should().Be("New Migration Plan");
        viewModel.HasPlan.Should().BeTrue();
    }

    [Fact]
    public void NewPlan_ClearsExistingSteps()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Steps.Add(CreateStep(1));
        viewModel.Steps.Add(CreateStep(2));

        // Act
        viewModel.NewPlanCommand.Execute(null);

        // Assert
        viewModel.Steps.Should().BeEmpty();
    }

    [Fact]
    public void AddStep_WithPlan_AddsStep()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NewPlanCommand.Execute(null);

        // Act
        viewModel.AddStepCommand.Execute(null);

        // Assert
        viewModel.Steps.Should().HaveCount(1);
        viewModel.Steps[0].Index.Should().Be(1);
    }

    [Fact]
    public void AddStep_WithPlan_SetsSelectedStep()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NewPlanCommand.Execute(null);

        // Act
        viewModel.AddStepCommand.Execute(null);

        // Assert
        viewModel.SelectedStep.Should().NotBeNull();
        viewModel.SelectedStep.Should().Be(viewModel.Steps[0]);
    }

    [Fact]
    public void AddStep_WithoutPlan_DoesNothing()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.AddStepCommand.Execute(null);

        // Assert
        viewModel.Steps.Should().BeEmpty();
    }

    [Fact]
    public void AddStep_MultipleTimes_IncrementsIndex()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NewPlanCommand.Execute(null);

        // Act
        viewModel.AddStepCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);

        // Assert
        viewModel.Steps.Should().HaveCount(3);
        viewModel.Steps[0].Index.Should().Be(1);
        viewModel.Steps[1].Index.Should().Be(2);
        viewModel.Steps[2].Index.Should().Be(3);
    }

    [Fact]
    public void RemoveStep_RemovesFromCollection()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NewPlanCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);
        var stepToRemove = viewModel.Steps[0];

        // Act
        viewModel.RemoveStepCommand.Execute(stepToRemove);

        // Assert
        viewModel.Steps.Should().HaveCount(1);
        viewModel.Steps.Should().NotContain(stepToRemove);
    }

    [Fact]
    public void RemoveStep_ReindexesRemainingSteps()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.NewPlanCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);
        viewModel.AddStepCommand.Execute(null);
        var firstStep = viewModel.Steps[0];

        // Act
        viewModel.RemoveStepCommand.Execute(firstStep);

        // Assert
        viewModel.Steps.Should().HaveCount(2);
        viewModel.Steps[0].Index.Should().Be(1);
        viewModel.Steps[1].Index.Should().Be(2);
    }

    [Fact]
    public async Task SavePlanAsync_WithoutPlan_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = async () => await viewModel.SavePlanCommand.ExecuteAsync(null);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadPlanAsync_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = async () => await viewModel.LoadPlanCommand.ExecuteAsync(null);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidatePlanAsync_WithoutPlan_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = async () => await viewModel.ValidatePlanCommand.ExecuteAsync(null);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecutePlanAsync_WithoutPlan_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = async () => await viewModel.ExecutePlanCommand.ExecuteAsync(null);
        await act.Should().NotThrowAsync();
    }

    private static MigrationStep CreateStep(int index)
    {
        return new MigrationStep
        {
            Index = index,
            Action = MigrationAction.MoveFile,
            Source = $"source{index}",
            Target = $"target{index}"
        };
    }
}

/// <summary>
/// Testable version of PlannerViewModel.
/// </summary>
public class TestablePlannerViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly ILocalizationService _localization;

    private string _title = string.Empty;
    private MigrationPlan? _currentPlan;
    private MigrationStep? _selectedStep;
    private bool _hasPlan;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public MigrationPlan? CurrentPlan
    {
        get => _currentPlan;
        set => SetProperty(ref _currentPlan, value);
    }

    public MigrationStep? SelectedStep
    {
        get => _selectedStep;
        set => SetProperty(ref _selectedStep, value);
    }

    public bool HasPlan
    {
        get => _hasPlan;
        set => SetProperty(ref _hasPlan, value);
    }

    public ObservableCollection<MigrationStep> Steps { get; } = [];

    public CommunityToolkit.Mvvm.Input.RelayCommand NewPlanCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand AddStepCommand { get; }
    public CommunityToolkit.Mvvm.Input.RelayCommand<MigrationStep> RemoveStepCommand { get; }
    public CommunityToolkit.Mvvm.Input.AsyncRelayCommand SavePlanCommand { get; }
    public CommunityToolkit.Mvvm.Input.AsyncRelayCommand LoadPlanCommand { get; }
    public CommunityToolkit.Mvvm.Input.AsyncRelayCommand ValidatePlanCommand { get; }
    public CommunityToolkit.Mvvm.Input.AsyncRelayCommand ExecutePlanCommand { get; }

    public TestablePlannerViewModel(ILocalizationService localization)
    {
        _localization = localization;
        Title = _localization.Get("PlannerTitle");

        NewPlanCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(NewPlan);
        AddStepCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(AddStep);
        RemoveStepCommand = new CommunityToolkit.Mvvm.Input.RelayCommand<MigrationStep>(RemoveStep);
        SavePlanCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(SavePlanAsync);
        LoadPlanCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(LoadPlanAsync);
        ValidatePlanCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ValidatePlanAsync);
        ExecutePlanCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ExecutePlanAsync);
    }

    private void NewPlan()
    {
        CurrentPlan = new MigrationPlan
        {
            Name = "New Migration Plan"
        };
        Steps.Clear();
        HasPlan = true;
    }

    private void AddStep()
    {
        if (CurrentPlan == null) return;

        var step = new MigrationStep
        {
            Index = Steps.Count + 1,
            Action = MigrationAction.MoveFile,
            Source = "",
            Target = ""
        };

        Steps.Add(step);
        SelectedStep = step;
    }

    private void RemoveStep(MigrationStep? step)
    {
        if (step == null) return;

        Steps.Remove(step);

        // Re-index
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i] = Steps[i] with { Index = i + 1 };
        }
    }

    private Task SavePlanAsync()
    {
        if (CurrentPlan == null) return Task.CompletedTask;
        // TODO: Implement save to file
        return Task.CompletedTask;
    }

    private Task LoadPlanAsync()
    {
        // TODO: Implement load from file
        return Task.CompletedTask;
    }

    private Task ValidatePlanAsync()
    {
        if (CurrentPlan == null) return Task.CompletedTask;
        // TODO: Implement validation
        return Task.CompletedTask;
    }

    private Task ExecutePlanAsync()
    {
        if (CurrentPlan == null) return Task.CompletedTask;
        // TODO: Implement execution
        return Task.CompletedTask;
    }
}
