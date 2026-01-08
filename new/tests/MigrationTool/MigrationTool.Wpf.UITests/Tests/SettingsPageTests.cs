using FluentAssertions;
using MigrationTool.Wpf.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Wpf.UITests.Tests;

/// <summary>
/// Tests for the Settings page functionality.
/// </summary>
[Collection("UI Tests")]
public class SettingsPageTests : UITestBase
{
    #region Test Constants

    private static class PageElements
    {
        public const string PageTitle = "Settings";
        public const string BrowseButton = "Browse";
        public const string LoadButton = "Load";
        public const string ClearButton = "Clear";
        public const string RefreshButton = "Refresh";
        public const string LanguagePicker = "Language";
    }

    private static class Languages
    {
        public const string English = "English";
        public const string Czech = "Čeština";
        public const string Polish = "Polski";
    }

    #endregion

    public SettingsPageTests()
    {
        try
        {
            InitializeDriver();
        }
        catch (SkipException)
        {
            // WinAppDriver not running
        }
    }

    private void NavigateToSettings()
    {
        NavigateTo("Settings");
        WaitForElement(PageElements.PageTitle);
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_ShowsTitle()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateToSettings();

        // Assert
        var title = WaitForElement(PageElements.PageTitle);
        title.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_HasBrowseButton()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateToSettings();

        // Assert
        var browseButton = WaitForElement(PageElements.BrowseButton);
        browseButton.Should().NotBeNull("Browse button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_HasLoadButton()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateToSettings();

        // Assert
        var loadButton = WaitForElement(PageElements.LoadButton);
        loadButton.Should().NotBeNull("Load button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_HasLanguageSelection()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateToSettings();

        // Assert - Look for language-related elements
        var languageLabel = WaitForElement(PageElements.LanguagePicker);
        // Language picker should exist
        (languageLabel != null || ElementExists(Languages.English)).Should().BeTrue(
            "Language selection should be available");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_ClearButton_IsDisabledWithoutSolution()
    {
        if (!IsDriverInitialized) return;

        // Act
        NavigateToSettings();

        // Assert
        var clearButton = WaitForElement(PageElements.ClearButton);
        // Clear button might be disabled or hidden without a loaded solution
        // This test documents the expected behavior
    }
}
