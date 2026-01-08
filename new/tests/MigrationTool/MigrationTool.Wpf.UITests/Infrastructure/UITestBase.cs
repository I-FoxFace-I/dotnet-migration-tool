using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;

namespace MigrationTool.Wpf.UITests.Infrastructure;

/// <summary>
/// Base class for WPF UI tests providing common functionality.
/// </summary>
public abstract class UITestBase : IDisposable
{
    #region Constants

    protected static class AutomationIds
    {
        public const string MainWindow = "MainWindow";
        public const string NavDashboard = "NavDashboard";
        public const string NavExplorer = "NavExplorer";
        public const string NavPlanner = "NavPlanner";
        public const string NavSettings = "NavSettings";
        public const string ContentArea = "ContentArea";
    }

    protected static class Timeouts
    {
        public static readonly TimeSpan Short = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan Medium = TimeSpan.FromSeconds(5);
        public static readonly TimeSpan Long = TimeSpan.FromSeconds(10);
    }

    #endregion

    protected WindowsDriver? Driver { get; private set; }
    protected bool IsDriverInitialized => Driver != null;

    #region Setup and Teardown

    protected void InitializeDriver()
    {
        if (!AppiumSetup.IsWinAppDriverRunning())
        {
            throw new SkipException("WinAppDriver is not running. Start WinAppDriver before running UI tests.");
        }

        Driver = AppiumSetup.CreateDriver();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                Driver?.Quit();
            }
            catch
            {
                // Ignore errors during cleanup
            }
            finally
            {
                Driver = null;
            }
        }
    }

    #endregion

    #region Navigation Helpers

    /// <summary>
    /// Navigates to a page using the sidebar navigation.
    /// </summary>
    protected void NavigateTo(string pageName)
    {
        if (Driver == null) return;

        var automationId = pageName switch
        {
            "Dashboard" => AutomationIds.NavDashboard,
            "Explorer" or "Project Explorer" => AutomationIds.NavExplorer,
            "Planner" or "Migration Planner" => AutomationIds.NavPlanner,
            "Settings" => AutomationIds.NavSettings,
            _ => throw new ArgumentException($"Unknown page: {pageName}")
        };

        try
        {
            var button = Driver.FindElement(By.Name(pageName));
            button.Click();
            Thread.Sleep(500); // Wait for navigation animation
        }
        catch (NoSuchElementException)
        {
            // Try by automation ID
            var button = Driver.FindElement(By.Id(automationId));
            button.Click();
            Thread.Sleep(500);
        }
    }

    #endregion

    #region Element Helpers

    /// <summary>
    /// Waits for an element with the specified text to appear.
    /// </summary>
    protected IWebElement? WaitForElement(string text, TimeSpan? timeout = null)
    {
        if (Driver == null) return null;

        var waitTime = timeout ?? Timeouts.Medium;
        var endTime = DateTime.Now.Add(waitTime);

        while (DateTime.Now < endTime)
        {
            try
            {
                var element = Driver.FindElement(By.Name(text));
                if (element.Displayed)
                {
                    return element;
                }
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue waiting
            }

            Thread.Sleep(100);
        }

        return null;
    }

    /// <summary>
    /// Waits for an element with the specified automation ID to appear.
    /// </summary>
    protected IWebElement? WaitForElementById(string automationId, TimeSpan? timeout = null)
    {
        if (Driver == null) return null;

        var waitTime = timeout ?? Timeouts.Medium;
        var endTime = DateTime.Now.Add(waitTime);

        while (DateTime.Now < endTime)
        {
            try
            {
                var element = Driver.FindElement(By.Id(automationId));
                if (element.Displayed)
                {
                    return element;
                }
            }
            catch (NoSuchElementException)
            {
                // Element not found yet, continue waiting
            }

            Thread.Sleep(100);
        }

        return null;
    }

    /// <summary>
    /// Finds an element by text content.
    /// </summary>
    protected IWebElement? FindElementByText(string text)
    {
        if (Driver == null) return null;

        try
        {
            return Driver.FindElement(By.Name(text));
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if an element with the specified text exists.
    /// </summary>
    protected bool ElementExists(string text)
    {
        return FindElementByText(text) != null;
    }

    /// <summary>
    /// Clicks a button with the specified text.
    /// </summary>
    protected void ClickButton(string buttonText)
    {
        if (Driver == null) return;

        var button = Driver.FindElement(By.Name(buttonText));
        button.Click();
    }

    /// <summary>
    /// Enters text into a text field.
    /// </summary>
    protected void EnterText(string automationId, string text)
    {
        if (Driver == null) return;

        var textBox = Driver.FindElement(By.Id(automationId));
        textBox.Clear();
        textBox.SendKeys(text);
    }

    #endregion
}

/// <summary>
/// Exception thrown when a test should be skipped.
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}
