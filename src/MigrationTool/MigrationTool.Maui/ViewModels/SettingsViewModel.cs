using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;
using MigrationTool.Maui.Services;
using MigrationTool.Maui.Services.Validation;

namespace MigrationTool.Maui.ViewModels;

/// <summary>
/// ViewModel for Settings page.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly AppState _appState;
    private readonly ViewModelBindingValidator _validator;

    [ObservableProperty]
    private string _solutionPath = string.Empty;

    [ObservableProperty]
    private LanguageOption? _selectedLanguageOption;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isLoadingSolution;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<LanguageOption> Languages { get; } = [];

    /// <summary>
    /// Gets whether a solution is currently loaded.
    /// </summary>
    public bool HasSolution => _appState.HasSolution;

    /// <summary>
    /// Gets the current solution name.
    /// </summary>
    public string? SolutionName => _appState.CurrentSolution?.Name;

    /// <summary>
    /// Gets the workspace path from AppState.
    /// </summary>
    public string WorkspacePath => _appState.WorkspacePath;

    public SettingsViewModel(
        ILocalizationService localization, 
        AppState appState,
        ViewModelBindingValidator validator) : base(localization)
    {
        _appState = appState;
        _validator = validator;
        Title = T(Strings.SettingsTitle);

        // Load available languages
        foreach (var lang in localization.SupportedLanguages)
        {
            Languages.Add(new LanguageOption(lang.Key, lang.Value));
        }

        // Set current language
        SelectedLanguageOption = Languages.FirstOrDefault(l => l.Code == localization.CurrentLanguage);

        // Subscribe to AppState changes
        _appState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(AppState.HasSolution))
            {
                OnPropertyChanged(nameof(HasSolution));
                OnPropertyChanged(nameof(SolutionName));
            }
            else if (e.PropertyName == nameof(AppState.CurrentSolution))
            {
                OnPropertyChanged(nameof(SolutionName));
            }
            else if (e.PropertyName == nameof(AppState.WorkspacePath))
            {
                OnPropertyChanged(nameof(WorkspacePath));
            }
            else if (e.PropertyName == nameof(AppState.IsLoading))
            {
                IsLoadingSolution = _appState.IsLoading;
            }
            else if (e.PropertyName == nameof(AppState.ErrorMessage))
            {
                StatusMessage = _appState.ErrorMessage;
            }
        };

        // Initialize from AppState
        SolutionPath = _appState.SolutionPath;
        IsLoadingSolution = _appState.IsLoading;
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (value != null)
        {
            Localization.CurrentLanguage = value.Code;
        }
    }

    [RelayCommand]
    private async Task BrowseSolutionAsync()
    {
        try
        {
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".sln" } },
                    { DevicePlatform.macOS, new[] { "sln" } },
                });

            var options = new PickOptions
            {
                PickerTitle = "Select Solution File",
                FileTypes = customFileType
            };

            var result = await FilePicker.Default.PickAsync(options);
            if (result != null)
            {
                SolutionPath = result.FullPath;
                StatusMessage = null;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error selecting file: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Error picking file: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        if (string.IsNullOrWhiteSpace(SolutionPath))
        {
            StatusMessage = "Please select a solution file first";
            return;
        }

        StatusMessage = "Loading solution...";
        await _appState.LoadSolutionAsync(SolutionPath);

        if (_appState.HasSolution)
        {
            StatusMessage = $"Loaded: {_appState.CurrentSolution?.Name} ({_appState.CurrentSolution?.ProjectCount} projects)";
        }
    }

    [RelayCommand]
    private void ClearSolution()
    {
        _appState.ClearSolution();
        SolutionPath = string.Empty;
        StatusMessage = "Solution cleared";
    }

    [RelayCommand]
    private async Task RefreshSolutionAsync()
    {
        if (_appState.HasSolution)
        {
            StatusMessage = "Refreshing solution...";
            await _appState.RefreshSolutionAsync();
            StatusMessage = "Solution refreshed";
        }
    }

    [RelayCommand]
    private async Task ValidateBindingsAsync()
    {
        StatusMessage = "Validating ViewModel bindings...";
        
        _validator.Clear();
        _validator.ValidateAssembly(typeof(SettingsViewModel).Assembly);
        
        var report = _validator.GetReport();
        
        // Get current page for displaying alert
        var currentPage = Application.Current?.MainPage;
        if (currentPage != null)
        {
            await _validator.ShowReportAsync(report, currentPage);
        }
        
        StatusMessage = report.HasErrors 
            ? $"❌ Validation failed: {report.FailedCount} issues found"
            : $"✅ Validation passed: {report.PassedCount} bindings OK";
    }
}

/// <summary>
/// Language option for picker.
/// </summary>
public record LanguageOption(string Code, string DisplayName);
