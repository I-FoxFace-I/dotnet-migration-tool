using System.ComponentModel;

namespace MigrationTool.Localization;

/// <summary>
/// Service for getting localized strings.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Current language code.
    /// </summary>
    string CurrentLanguage { get; set; }

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    string Get(string key);

    /// <summary>
    /// Gets a localized string by key with format parameters.
    /// </summary>
    string Get(string key, params object[] args);

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    IReadOnlyDictionary<string, string> SupportedLanguages { get; }
}

/// <summary>
/// Singleton implementation of ILocalizationService.
/// Can be used directly in XAML via static Instance property.
/// </summary>
public class LocalizationService : ILocalizationService
{
    /// <summary>
    /// Singleton instance for direct access in XAML and code.
    /// </summary>
    public static LocalizationService Instance { get; } = new();

    private string _currentLanguage = "en";

    /// <summary>
    /// Event raised when language changes (for UI refresh).
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (Translations.SupportedLanguages.ContainsKey(value) && _currentLanguage != value)
            {
                _currentLanguage = value;
                // Notify all bindings to refresh
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> SupportedLanguages => Translations.SupportedLanguages;

    /// <summary>
    /// Indexer for easy XAML binding: {Binding [KeyName], Source={x:Static loc:LocalizationService.Instance}}
    /// </summary>
    public string this[string key] => Get(key);

    /// <inheritdoc />
    public string Get(string key)
    {
        if (Translations.All.TryGetValue(_currentLanguage, out var translations) &&
            translations.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fallback to English
        if (Translations.All.TryGetValue("en", out var englishTranslations) &&
            englishTranslations.TryGetValue(key, out var englishValue))
        {
            return englishValue;
        }

        return key; // Return key if not found
    }

    /// <inheritdoc />
    public string Get(string key, params object[] args)
    {
        var template = Get(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }
}
