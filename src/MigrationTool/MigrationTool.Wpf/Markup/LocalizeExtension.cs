using System.Windows.Markup;
using MigrationTool.Localization;

namespace MigrationTool.Wpf.Markup;

/// <summary>
/// XAML MarkupExtension for easy localization.
/// Usage: Text="{loc:Localize DashboardTitle}"
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class LocalizeExtension : MarkupExtension
{
    /// <summary>
    /// The localization key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new LocalizeExtension.
    /// </summary>
    public LocalizeExtension()
    {
    }

    /// <summary>
    /// Creates a new LocalizeExtension with the specified key.
    /// </summary>
    public LocalizeExtension(string key)
    {
        Key = key;
    }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        return LocalizationService.Instance.Get(Key);
    }
}

/// <summary>
/// Static helper class for localization access in code.
/// Usage: L.Get("KeyName") or L.T("KeyName")
/// </summary>
public static class L
{
    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    public static string Get(string key) => LocalizationService.Instance.Get(key);

    /// <summary>
    /// Gets a localized string by key (alias for Get).
    /// </summary>
    public static string T(string key) => Get(key);

    /// <summary>
    /// Gets a localized string by key with format parameters.
    /// </summary>
    public static string Get(string key, params object[] args) => LocalizationService.Instance.Get(key, args);

    /// <summary>
    /// Gets or sets the current language.
    /// </summary>
    public static string CurrentLanguage
    {
        get => LocalizationService.Instance.CurrentLanguage;
        set => LocalizationService.Instance.CurrentLanguage = value;
    }

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    public static IReadOnlyDictionary<string, string> SupportedLanguages =>
        LocalizationService.Instance.SupportedLanguages;
}
