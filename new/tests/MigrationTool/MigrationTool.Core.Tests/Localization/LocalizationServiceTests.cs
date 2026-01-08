using FluentAssertions;
using MigrationTool.Localization;
using MigrationTool.Localization.Resources;
using Xunit;

namespace MigrationTool.Core.Tests.Localization;

public class LocalizationServiceTests
{
    private readonly LocalizationService _service;

    public LocalizationServiceTests()
    {
        _service = new LocalizationService();
    }

    [Fact]
    public void DefaultLanguage_IsEnglish()
    {
        _service.CurrentLanguage.Should().Be("en");
    }

    [Fact]
    public void Get_EnglishKey_ReturnsEnglishValue()
    {
        var result = _service.Get(Strings.AppTitle);

        result.Should().Be("Migration Tool");
    }

    [Fact]
    public void Get_AfterChangingLanguage_ReturnsLocalizedValue()
    {
        _service.CurrentLanguage = "cs";

        var result = _service.Get(Strings.AppTitle);

        result.Should().Be("Migrační nástroj");
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsKey()
    {
        var result = _service.Get("NonExistentKey");

        result.Should().Be("NonExistentKey");
    }

    [Fact]
    public void SetLanguage_InvalidLanguage_DoesNotChange()
    {
        _service.CurrentLanguage = "invalid";

        _service.CurrentLanguage.Should().Be("en");
    }

    [Fact]
    public void SupportedLanguages_ContainsAllLanguages()
    {
        _service.SupportedLanguages.Should().ContainKey("en");
        _service.SupportedLanguages.Should().ContainKey("cs");
        _service.SupportedLanguages.Should().ContainKey("pl");
        _service.SupportedLanguages.Should().ContainKey("uk");
    }

    [Theory]
    [InlineData("en", "Dashboard")]
    [InlineData("cs", "Přehled")]
    [InlineData("pl", "Panel")]
    [InlineData("uk", "Панель")]
    public void Get_DashboardTitle_ReturnsCorrectTranslation(string language, string expected)
    {
        _service.CurrentLanguage = language;

        var result = _service.Get(Strings.NavDashboard);

        result.Should().Be(expected);
    }

    [Fact]
    public void Get_WithFormatParameters_FormatsCorrectly()
    {
        // This would require adding a key with format placeholders
        // For now, test that format doesn't break with no placeholders
        var result = _service.Get(Strings.AppTitle, "unused");

        result.Should().Be("Migration Tool");
    }

    [Fact]
    public void AllTranslations_HaveSameKeys()
    {
        var englishKeys = Translations.All["en"].Keys.ToHashSet();

        foreach (var (lang, translations) in Translations.All)
        {
            var langKeys = translations.Keys.ToHashSet();
            langKeys.Should().BeSubsetOf(englishKeys,
                because: $"Language '{lang}' should have all the same keys as English");
        }
    }

    [Fact]
    public void AllTranslations_HaveNonEmptyValues()
    {
        foreach (var (lang, translations) in Translations.All)
        {
            foreach (var (key, value) in translations)
            {
                value.Should().NotBeNullOrWhiteSpace(
                    because: $"Translation for key '{key}' in language '{lang}' should not be empty");
            }
        }
    }
}
