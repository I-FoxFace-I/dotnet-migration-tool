using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.DependencyInjection;
using MigrationTool.Localization;
using MigrationTool.Wpf.Services;
using MigrationTool.Wpf.ViewModels;
using MigrationTool.Wpf.Views;
using Wpf.Services.MicrosoftDI.Configuration;

namespace MigrationTool.Wpf;

/// <summary>
/// MigrationTool WPF Application
/// </summary>
public partial class App : Application
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllocConsole();

    private IServiceProvider? _serviceProvider;
    private BindingErrorTraceListener? _bindingErrorListener;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            // Enable debug console in DEBUG mode
#if DEBUG
            AllocConsole();
            Console.WriteLine("=== MigrationTool WPF v1.0.0 ===");
            Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            EnableBindingErrorDetection();
#endif

            ConfigureServices();
            ShowMainWindow();

            Console.WriteLine("✅ Application started successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Application startup failed: {ex.Message}");
            MessageBox.Show(
                $"Application startup failed:\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <summary>
    /// Enables WPF binding error detection - shows errors in Debug output and optionally throws exceptions.
    /// </summary>
    private void EnableBindingErrorDetection()
    {
        // Set binding trace level to Error
        PresentationTraceSources.DataBindingSource.Listeners.Add(
            _bindingErrorListener = new BindingErrorTraceListener());
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;

        Console.WriteLine("🔍 WPF Binding Error Detection Enabled");
    }

    private void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // WPF Framework services
        services.AddWpfServices();

        // MigrationTool Core services
        services.AddMigrationToolCore();

        // Localization - register singleton instance for DI (optional, can use L.Get() directly)
        services.AddSingleton<ILocalizationService>(LocalizationService.Instance);

        // App State (with solution analyzer for loading solutions)
        services.AddSingleton<AppState>(sp => 
            new AppState(sp.GetRequiredService<MigrationTool.Core.Abstractions.Services.ISolutionAnalyzer>()));
        services.AddSingleton<IAppState>(sp => sp.GetRequiredService<AppState>());

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ExplorerViewModel>();
        services.AddTransient<PlannerViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<DashboardView>();
        services.AddTransient<ExplorerView>();
        services.AddTransient<PlannerView>();
        services.AddTransient<SettingsView>();

        _serviceProvider = services.BuildServiceProvider();
    }

    private void ShowMainWindow()
    {
        if (_serviceProvider == null) return;

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}
