using FluentAssertions;
using MigrationTool.Maui.UITests.Infrastructure;
using Xunit;

namespace MigrationTool.Maui.UITests.Tests;

/// <summary>
/// Tests for the Settings page functionality.
/// </summary>
[Collection("UI Tests")]
public class SettingsPageTests : UITestBase
{
    #region Test Constants

    private static class PageElements
    {
        public const string SettingsTitle = "Settings";
        public const string WorkspacePathLabel = "Workspace Path";
        public const string BrowseButton = "Browse";
        public const string LanguageLabel = "Language";
    }

    private static class Languages
    {
        public const string English = "English";
        public const string Czech = "Čeština";
        public const string Polish = "Polski";
        public const string Ukrainian = "Українська";
    }

    private static class TestPaths
    {
        public const string ValidPath = @"C:\Projects\TestSolution";
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

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_ShowsWorkspacePathInput()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.SettingsTitle);

        // Assert
        var workspaceLabel = TryFindByName(PageElements.WorkspacePathLabel);
        workspaceLabel.Should().NotBeNull("Workspace Path label should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_ShowsBrowseButton()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.SettingsTitle);

        // Assert
        var browseButton = TryFindByName(PageElements.BrowseButton);
        browseButton.Should().NotBeNull("Browse button should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_ShowsLanguageSelector()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.SettingsTitle);

        // Assert
        var languageLabel = TryFindByName(PageElements.LanguageLabel);
        languageLabel.Should().NotBeNull("Language label should be visible");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_LanguageSelector_ContainsEnglish()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.SettingsTitle);

        // Assert
        var englishOption = TryFindByName(Languages.English);
        englishOption.Should().NotBeNull("English language option should be available");
    }

    [Fact]
    [Trait("Category", "UI")]
    [Trait("Category", "Settings")]
    public void SettingsPage_BrowseButton_IsClickable()
    {
        if (!IsDriverInitialized) return;

        // Arrange
        NavigateTo(PageElements.SettingsTitle);
        var browseButton = TryFindByName(PageElements.BrowseButton);

        // Assert
        browseButton.Should().NotBeNull();
        browseButton!.Enabled.Should().BeTrue("Browse button should be enabled");
    }
}
