using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization.Resources;
using MigrationTool.Wpf.Markup;
using MigrationTool.Wpf.Services;
using Wpf.ViewModels.Base;

namespace MigrationTool.Wpf.ViewModels;

/// <summary>
/// ViewModel for Planner view.
/// </summary>
public partial class PlannerViewModel : BaseViewModel
{
    private readonly IAppState _appState;
    private readonly IMigrationPlanner _migrationPlanner;
    private readonly IMigrationExecutor _migrationExecutor;

    [ObservableProperty]
    private MigrationPlan? _currentPlan;

    [ObservableProperty]
    private ObservableCollection<MigrationStep> _steps = [];

    [ObservableProperty]
    private MigrationStep? _selectedStep;

    [ObservableProperty]
    private string _planName = "New Migration Plan";

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private double _executionProgress;

    [ObservableProperty]
    private string? _statusMessage;

    public bool HasPlan => CurrentPlan != null;
    public bool HasSolution => _appState.HasSolution;

    public PlannerViewModel(
        ILogger<PlannerViewModel> logger,
        IAppState appState,
        IMigrationPlanner migrationPlanner,
        IMigrationExecutor migrationExecutor) : base(logger)
    {
        _appState = appState;
        _migrationPlanner = migrationPlanner;
        _migrationExecutor = migrationExecutor;
        DisplayName = L.Get(Strings.PlannerTitle);

        _appState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AppState.HasSolution))
            {
                OnPropertyChanged(nameof(HasSolution));
            }
        };
    }

    [RelayCommand]
    private void NewPlan()
    {
        CurrentPlan = _migrationPlanner.CreatePlan(PlanName);
        Steps.Clear();
        StatusMessage = "New plan created";
        OnPropertyChanged(nameof(HasPlan));
    }

    [RelayCommand]
    private void AddStep()
    {
        if (CurrentPlan == null)
        {
            NewPlan();
        }

        var step = new MigrationStep
        {
            Index = Steps.Count + 1,
            Action = MigrationAction.MoveFile,
            Source = string.Empty,
            Target = string.Empty
        };

        CurrentPlan = _migrationPlanner.AddStep(CurrentPlan!, step);
        Steps.Add(step);
        SelectedStep = step;
    }

    [RelayCommand]
    private void RemoveStep()
    {
        if (SelectedStep == null || CurrentPlan == null) return;

        var index = Steps.IndexOf(SelectedStep);
        if (index >= 0)
        {
            CurrentPlan = _migrationPlanner.RemoveStep(CurrentPlan, index);
            Steps.RemoveAt(index);
            SelectedStep = Steps.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void MoveStepUp()
    {
        if (SelectedStep == null) return;

        var index = Steps.IndexOf(SelectedStep);
        if (index > 0)
        {
            Steps.Move(index, index - 1);
            RebuildPlanFromSteps();
        }
    }

    [RelayCommand]
    private void MoveStepDown()
    {
        if (SelectedStep == null) return;

        var index = Steps.IndexOf(SelectedStep);
        if (index < Steps.Count - 1)
        {
            Steps.Move(index, index + 1);
            RebuildPlanFromSteps();
        }
    }

    private void RebuildPlanFromSteps()
    {
        if (CurrentPlan == null) return;

        CurrentPlan = CurrentPlan with { Steps = Steps.ToList() };
    }

    [RelayCommand]
    private void SavePlan()
    {
        if (CurrentPlan == null) return;

        var dialog = new SaveFileDialog
        {
            Title = "Save Migration Plan",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = $"{PlanName.Replace(" ", "_")}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = _migrationPlanner.ExportPlan(CurrentPlan);
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                StatusMessage = $"Plan saved to {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving plan: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void LoadPlan()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Migration Plan",
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                CurrentPlan = _migrationPlanner.ImportPlan(json);
                PlanName = CurrentPlan.Name;
                Steps = new ObservableCollection<MigrationStep>(CurrentPlan.Steps);
                OnPropertyChanged(nameof(HasPlan));
                StatusMessage = $"Plan loaded from {dialog.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading plan: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ExecutePlanAsync()
    {
        if (CurrentPlan == null) return;

        try
        {
            IsExecuting = true;
            ExecutionProgress = 0;
            StatusMessage = "Executing migration plan...";

            var progress = new Progress<MigrationProgress>(p =>
            {
                ExecutionProgress = p.PercentComplete;
                StatusMessage = $"Step {p.CurrentStep}/{p.TotalSteps}: {p.CurrentAction}";
            });

            var result = await _migrationExecutor.ExecuteAsync(CurrentPlan, progress);

            if (result.Success)
            {
                StatusMessage = $"✅ Migration completed: {result.StepResults.Count(s => s.Success)} steps executed";
            }
            else
            {
                StatusMessage = $"❌ Migration failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
