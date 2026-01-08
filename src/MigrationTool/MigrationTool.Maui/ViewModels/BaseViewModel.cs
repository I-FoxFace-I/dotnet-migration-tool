using CommunityToolkit.Mvvm.ComponentModel;
using MigrationTool.Localization;

namespace MigrationTool.Maui.ViewModels;

/// <summary>
/// Base class for all ViewModels with common functionality.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    protected readonly ILocalizationService Localization;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    protected BaseViewModel(ILocalizationService localization)
    {
        Localization = localization;
    }

    /// <summary>
    /// Gets a localized string.
    /// </summary>
    protected string T(string key) => Localization.Get(key);
}
