using Bunit;
using FluentAssertions;
using MigrationTool.Blazor.Server.Components.Shared;
using MigrationTool.Localization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MigrationTool.Blazor.Server.Tests.Components.Shared;

public class LanguageSelectorTests : TestContext
{
    private readonly ILocalizationService _localization;

    public LanguageSelectorTests()
    {
        _localization = new LocalizationService();
        Services.AddSingleton<ILocalizationService>(_localization);
    }

    [Fact]
    public void Render_ShowsLanguageDropdown()
    {
        // Act
        var cut = RenderComponent<LanguageSelector>();

        // Assert
        cut.Markup.Should().Contain("language-selector");
        cut.Markup.Should().Contain("select");
    }

    [Fact]
    public void Render_ShowsAllSupportedLanguages()
    {
        // Act
        var cut = RenderComponent<LanguageSelector>();

        // Assert
        foreach (var lang in _localization.SupportedLanguages)
        {
            cut.Markup.Should().Contain(lang.Value);
        }
    }

    [Fact]
    public void ChangeLanguage_UpdatesLocalizationService()
    {
        // Arrange
        var cut = RenderComponent<LanguageSelector>();
        var initialLanguage = _localization.CurrentLanguage;

        // Act
        var select = cut.Find("select");
        select.Change("cs");

        // Assert
        _localization.CurrentLanguage.Should().Be("cs");
    }
}
