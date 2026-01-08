using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;
using MigrationTool.Maui.Services;

namespace MigrationTool.Maui.ViewModels;

/// <summary>
/// ViewModel for Migration Planner page.
/// </summary>
public partial class PlannerViewModel : BaseViewModel
{
    private readonly AppState _appState;
    private readonly IMigrationPlanner _planner;
    private readonly IMigrationExecutor _executor;

    [ObservableProperty]
    private MigrationPlan? _currentPlan;

    [ObservableProperty]
    private MigrationStep? _selectedStep;

    [ObservableProperty]
    private bool _hasPlan;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isExecuting;

    public ObservableCollection<MigrationStep> Steps { get; } = [];

    /// <summary>
    /// Gets whether a solution is loaded.
    /// </summary>
    public bool HasSolution => _appState.HasSolution;

    public PlannerViewModel(
        ILocalizationService localization,
        AppState appState,
        IMigrationPlanner planner,
        IMigrationExecutor executor) : base(localization)
    {
        _appState = appState;
        _planner = planner;
        _executor = executor;
        Title = T(Strings.PlannerTitle);

        // Subscribe to AppState changes
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
        CurrentPlan = _planner.CreatePlan("New Migration Plan");
        Steps.Clear();
        HasPlan = true;
        StatusMessage = "New plan created";
    }

    [RelayCommand]
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
        StatusMessage = $"Added step {step.Index}";
    }

    [RelayCommand]
    private void RemoveStep(MigrationStep step)
    {
        Steps.Remove(step);

        // Re-index
        for (int i = 0; i < Steps.Count; i++)
        {
            Steps[i] = Steps[i] with { Index = i + 1 };
        }

        StatusMessage = "Step removed";
    }

    [RelayCommand]
    private async Task SavePlanAsync()
    {
        if (CurrentPlan == null)
        {
            StatusMessage = "No plan to save";
            return;
        }

        try
        {
            var planToSave = CurrentPlan with { Steps = Steps.ToList() };
            var json = _planner.ExportPlan(planToSave);

            // Use simple file save dialog approach
            var fileName = $"{planToSave.Name.Replace(" ", "_")}.json";
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsPath, fileName);

            await File.WriteAllTextAsync(filePath, json);
            StatusMessage = $"Plan saved to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadPlanAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".json" } },
                    { DevicePlatform.macOS, new[] { "json" } },
                });

            var options = new PickOptions
            {
                PickerTitle = "Select Migration Plan",
                FileTypes = customFileType
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                var json = await File.ReadAllTextAsync(result.FullPath);
                CurrentPlan = _planner.ImportPlan(json);
                
                Steps.Clear();
                foreach (var step in CurrentPlan.Steps)
                {
                    Steps.Add(step);
                }

                HasPlan = true;
                StatusMessage = $"Loaded: {CurrentPlan.Name}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ValidatePlanAsync()
    {
        if (CurrentPlan == null)
        {
            StatusMessage = "No plan to validate";
            return;
        }

        try
        {
            IsBusy = true;
            var planToValidate = CurrentPlan with { Steps = Steps.ToList() };
            var result = await _planner.ValidatePlanAsync(planToValidate);

            if (result.IsValid)
            {
                StatusMessage = "✅ Plan is valid";
            }
            else
            {
                StatusMessage = $"❌ Validation failed: {string.Join(", ", result.Errors)}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error validating: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExecutePlanAsync()
    {
        if (CurrentPlan == null || Steps.Count == 0)
        {
            StatusMessage = "No plan or steps to execute";
            return;
        }

        try
        {
            IsExecuting = true;
            Progress = 0;
            StatusMessage = "Executing plan...";

            var planToExecute = CurrentPlan with { Steps = Steps.ToList() };

            var progressReporter = new Progress<MigrationProgress>(p =>
            {
                Progress = p.PercentComplete;
                StatusMessage = $"Step {p.CurrentStep}/{p.TotalSteps}: {p.CurrentAction}";
            });

            var result = await _executor.ExecuteAsync(planToExecute, progressReporter);

            if (result.Success)
            {
                var completedCount = result.StepResults.Count(r => r.Success);
                StatusMessage = $"✅ Plan executed successfully! {completedCount} steps completed.";
                Progress = 100;

                // Refresh solution to reflect changes
                await _appState.RefreshSolutionAsync();
            }
            else
            {
                StatusMessage = $"❌ Execution failed: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error executing: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }
}
