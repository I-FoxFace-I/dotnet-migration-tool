using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using Xunit;

namespace MigrationTool.Maui.UITests.Infrastructure;

/// <summary>
/// Base class for UI tests providing common setup and teardown.
/// </summary>
public abstract class UITestBase : IDisposable
{
    #region Constants

    protected static class Timeouts
    {
        public static readonly TimeSpan Short = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan Medium = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan Long = TimeSpan.FromSeconds(10);
    }

    protected static class AutomationIds
    {
        // Shell/Navigation
        public const string FlyoutMenu = "FlyoutMenu";
        public const string DashboardItem = "Dashboard";
        public const string ExplorerItem = "Project Explorer";
        public const string PlannerItem = "Migration Planner";
        public const string SettingsItem = "Settings";

        // Dashboard page
        public const string ProjectCountLabel = "ProjectCount";
        public const string TestProjectCountLabel = "TestProjectCount";
        public const string SourceProjectCountLabel = "SourceProjectCount";
        public const string FileCountLabel = "FileCount";
        public const string ClassCountLabel = "ClassCount";
        public const string TestCountLabel = "TestCount";

        // Settings page
        public const string WorkspacePathEntry = "WorkspacePath";
        public const string BrowseButton = "BrowseWorkspace";
        public const string LanguagePicker = "LanguagePicker";

        // Planner page
        public const string NewPlanButton = "NewPlan";
        public const string AddStepButton = "AddStep";
        public const string SavePlanButton = "SavePlan";
        public const string ExecutePlanButton = "ExecutePlan";
    }

    #endregion

    protected WindowsDriver? Driver { get; private set; }
    protected bool IsDriverInitialized => Driver != null;

    /// <summary>
    /// Initializes the driver. Call this in test constructor or setup.
    /// </summary>
    protected void InitializeDriver()
    {
        if (!AppiumSetup.IsWinAppDriverRunning())
        {
            throw new SkipException("WinAppDriver is not running. Start it with: WinAppDriver.exe");
        }

        Driver = AppiumSetup.CreateDriver();
    }

    /// <summary>
    /// Finds an element by AutomationId.
    /// </summary>
    protected IWebElement FindByAutomationId(string automationId)
    {
        return Driver!.FindElement(By.Name(automationId));
    }

    /// <summary>
    /// Finds an element by Name.
    /// </summary>
    protected IWebElement FindByName(string name)
    {
        return Driver!.FindElement(By.Name(name));
    }

    /// <summary>
    /// Tries to find an element, returns null if not found.
    /// </summary>
    protected IWebElement? TryFindByName(string name)
    {
        try
        {
            return Driver!.FindElement(By.Name(name));
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Waits for an element to be visible.
    /// </summary>
    protected IWebElement WaitForElement(string name, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Timeouts.Medium;
        var endTime = DateTime.Now.Add(wait);

        while (DateTime.Now < endTime)
        {
            var element = TryFindByName(name);
            if (element != null && element.Displayed)
            {
                return element;
            }
            Thread.Sleep(100);
        }

        throw new TimeoutException($"Element '{name}' not found within {wait.TotalSeconds} seconds");
    }

    /// <summary>
    /// Clicks on an element by name.
    /// </summary>
    protected void ClickByName(string name)
    {
        var element = FindByName(name);
        element.Click();
    }

    /// <summary>
    /// Gets text from an element by name.
    /// </summary>
    protected string GetTextByName(string name)
    {
        var element = FindByName(name);
        return element.Text;
    }

    /// <summary>
    /// Opens the flyout menu (hamburger menu).
    /// </summary>
    protected void OpenFlyoutMenu()
    {
        // In MAUI Shell, the hamburger button usually has this automation ID
        var menuButton = TryFindByName("OK") ?? TryFindByName("NavigationViewBackButton");
        menuButton?.Click();
        Thread.Sleep(500); // Wait for animation
    }

    /// <summary>
    /// Navigates to a page using the flyout menu.
    /// </summary>
    protected void NavigateTo(string pageName)
    {
        OpenFlyoutMenu();
        Thread.Sleep(300);
        ClickByName(pageName);
        Thread.Sleep(500); // Wait for navigation
    }

    public void Dispose()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }
}

/// <summary>
/// Exception to skip tests when prerequisites are not met.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
