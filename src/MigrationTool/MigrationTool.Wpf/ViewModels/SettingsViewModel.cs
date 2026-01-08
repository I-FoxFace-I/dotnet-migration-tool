using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;
using MigrationTool.Wpf.Markup;
using MigrationTool.Wpf.Services;
using Wpf.ViewModels.Base;

namespace MigrationTool.Wpf.ViewModels;

/// <summary>
/// ViewModel for Settings view.
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IAppState _appState;

    [ObservableProperty]
    private string _solutionPath = string.Empty;

    [ObservableProperty]
    private LanguageOption? _selectedLanguageOption;

    [ObservableProperty]
    private bool _isLoadingSolution;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<LanguageOption> Languages { get; } = [];

    public bool HasSolution => _appState.HasSolution;
    public string? SolutionName => _appState.CurrentSolution?.Name;
    public string WorkspacePath => _appState.WorkspacePath;

    public SettingsViewModel(
        ILogger<SettingsViewModel> logger,
        IAppState appState) : base(logger)
    {
        _appState = appState;
        DisplayName = L.Get(Strings.SettingsTitle);

        Console.WriteLine($"[SettingsViewModel] Initialized");

        foreach (var lang in L.SupportedLanguages)
        {
            Languages.Add(new LanguageOption(lang.Key, lang.Value));
        }

        SelectedLanguageOption = Languages.FirstOrDefault(l => l.Code == L.CurrentLanguage);

        _appState.PropertyChanged += (s, e) =>
        {
            Console.WriteLine($"[SettingsViewModel] AppState.{e.PropertyName} changed");
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

        SolutionPath = _appState.SolutionPath;
        IsLoadingSolution = _appState.IsLoading;
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (value != null)
        {
            L.CurrentLanguage = value.Code;
        }
    }

    [RelayCommand]
    private void BrowseSolution()
    {
        var dialog = new OpenFileDialog
        {
            Title = L.Get(Strings.SelectSolution),
            Filter = "Solution Files (*.sln)|*.sln|All Files (*.*)|*.*",
            DefaultExt = ".sln"
        };

        if (dialog.ShowDialog() == true)
        {
            SolutionPath = dialog.FileName;
            StatusMessage = null;
        }
    }

    [RelayCommand]
    private async Task LoadSolutionAsync()
    {
        Console.WriteLine($"[SettingsViewModel] LoadSolutionAsync called, path: {SolutionPath}");
        
        if (string.IsNullOrWhiteSpace(SolutionPath))
        {
            StatusMessage = L.Get(Strings.SelectWorkspace);
            Console.WriteLine($"[SettingsViewModel] No path specified");
            return;
        }

        StatusMessage = L.Get(Strings.Loading);
        await _appState.LoadSolutionAsync(SolutionPath);

        if (_appState.HasSolution)
        {
            StatusMessage = $"{L.Get(Strings.Success)}: {_appState.CurrentSolution?.Name} ({_appState.CurrentSolution?.ProjectCount} projects)";
            Console.WriteLine($"[SettingsViewModel] Solution loaded: {_appState.CurrentSolution?.Name} with {_appState.CurrentSolution?.ProjectCount} projects");
        }
        else
        {
            Console.WriteLine($"[SettingsViewModel] Failed to load solution: {_appState.ErrorMessage}");
        }
    }

    [RelayCommand]
    private void ClearSolution()
    {
        _appState.ClearSolution();
        SolutionPath = string.Empty;
        StatusMessage = L.Get(Strings.Success);
    }

    [RelayCommand]
    private async Task RefreshSolutionAsync()
    {
        if (_appState.HasSolution)
        {
            StatusMessage = L.Get(Strings.Loading);
            await _appState.RefreshSolutionAsync();
            StatusMessage = L.Get(Strings.Success);
        }
    }
}

/// <summary>
/// Language option for picker.
/// </summary>
public record LanguageOption(string Code, string DisplayName);
