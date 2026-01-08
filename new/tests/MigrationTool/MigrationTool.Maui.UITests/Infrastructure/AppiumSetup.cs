using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace MigrationTool.Maui.UITests.Infrastructure;

/// <summary>
/// Setup and configuration for Appium/WinAppDriver UI tests.
/// Requires WinAppDriver to be running: https://github.com/microsoft/WinAppDriver
/// </summary>
public static class AppiumSetup
{
    #region Configuration Constants

    public static class Config
    {
        /// <summary>
        /// WinAppDriver server URL (default port 4723).
        /// </summary>
        public const string WinAppDriverUrl = "http://127.0.0.1:4723";

        /// <summary>
        /// Path to the MAUI application executable.
        /// </summary>
        public static readonly string AppPath = GetAppPath();

        /// <summary>
        /// Timeout for finding elements.
        /// </summary>
        public static readonly TimeSpan ImplicitWait = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Timeout for app startup.
        /// </summary>
        public static readonly TimeSpan AppLaunchTimeout = TimeSpan.FromSeconds(30);

        private static string GetAppPath()
        {
            // Path relative to test project output
            var basePath = AppContext.BaseDirectory;
            var appPath = Path.GetFullPath(Path.Combine(basePath, 
                "..", "..", "..", "..", "..", 
                "src", "MigrationTool", "MigrationTool.Maui", 
                "bin", "Debug", "net9.0-windows10.0.19041.0", "win10-x64", 
                "MigrationTool.Maui.exe"));
            
            return appPath;
        }
    }

    #endregion

    #region Driver Factory

    /// <summary>
    /// Creates a new WindowsDriver instance for the Migration Tool application.
    /// </summary>
    public static WindowsDriver CreateDriver()
    {
        var options = new AppiumOptions
        {
            App = Config.AppPath,
            PlatformName = "Windows",
            AutomationName = "Windows"
        };

        options.AddAdditionalAppiumOption("ms:waitForAppLaunch", Config.AppLaunchTimeout.TotalSeconds);

        var driver = new WindowsDriver(new Uri(Config.WinAppDriverUrl), options);
        driver.Manage().Timeouts().ImplicitWait = Config.ImplicitWait;

        return driver;
    }

    /// <summary>
    /// Checks if WinAppDriver is running.
    /// </summary>
    public static bool IsWinAppDriverRunning()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(2);
            var response = client.GetAsync($"{Config.WinAppDriverUrl}/status").Result;
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
