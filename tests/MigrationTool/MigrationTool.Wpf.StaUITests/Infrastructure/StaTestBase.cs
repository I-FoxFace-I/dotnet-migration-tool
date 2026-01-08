using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.DependencyInjection;
using MigrationTool.Localization;
using MigrationTool.Wpf.Converters;
using MigrationTool.Wpf.Services;
using MigrationTool.Wpf.ViewModels;
using MigrationTool.Wpf.Views;
using Wpf.Services.MicrosoftDI.Configuration;

namespace MigrationTool.Wpf.StaUITests.Infrastructure;

/// <summary>
/// Base class for STA UI tests that creates a proper WPF environment.
/// </summary>
public abstract class StaTestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; }
    protected Application Application { get; }

    private static Application? _sharedApplication;
    private static readonly object _applicationLock = new();

    protected StaTestBase()
    {
        // Use singleton Application instance (WPF allows only one per AppDomain)
        lock (_applicationLock)
        {
            if (_sharedApplication == null)
            {
                _sharedApplication = new Application();
                
                // Initialize resources by loading Styles.xaml using ResourceDictionary with Source
                // This is the same approach used in App.xaml
                try
                {
                    var stylesDict = new ResourceDictionary
                    {
                        Source = new Uri("/MigrationTool.Wpf;component/Resources/Styles.xaml", UriKind.Relative)
                    };
                    _sharedApplication.Resources.MergedDictionaries.Add(stylesDict);
                }
                catch (Exception ex)
                {
                    // Fallback: try with pack URI
                    try
                    {
                        var stylesDict = new ResourceDictionary
                        {
                            Source = new Uri("pack://application:,,,/MigrationTool.Wpf;component/Resources/Styles.xaml", UriKind.Absolute)
                        };
                        _sharedApplication.Resources.MergedDictionaries.Add(stylesDict);
                    }
                    catch
                    {
                        // Last resort: manually create all required resources
                        _sharedApplication.Resources.Add("BoolToVisibility", new System.Windows.Controls.BooleanToVisibilityConverter());
                        _sharedApplication.Resources.Add("InverseBoolToVisibility", new InverseBooleanToVisibilityConverter());
                        _sharedApplication.Resources.Add("NullToVisibility", new Converters.NullToVisibilityConverter());
                        // Note: Styles like NavButtonStyle would need to be manually created if needed
                    }
                }
            }
            
            Application = _sharedApplication;
        }
        
        // Configure services (same as App.xaml.cs)
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

        // Localization
        services.AddSingleton<ILocalizationService>(LocalizationService.Instance);

        // App State
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

        ServiceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Creates a view with its ViewModel from DI.
    /// </summary>
    protected TView CreateView<TView>() where TView : FrameworkElement
    {
        return ServiceProvider.GetRequiredService<TView>();
    }

    /// <summary>
    /// Creates a ViewModel from DI.
    /// </summary>
    protected TViewModel CreateViewModel<TViewModel>() where TViewModel : class
    {
        return ServiceProvider.GetRequiredService<TViewModel>();
    }

    /// <summary>
    /// Processes WPF dispatcher queue to ensure UI updates are complete.
    /// </summary>
    protected void ProcessDispatcherQueue()
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Finds a child element by type in the visual tree.
    /// </summary>
    protected static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T found)
                return found;

            var childOfChild = FindChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }

        return null;
    }

    /// <summary>
    /// Finds all child elements by type in the visual tree.
    /// </summary>
    protected static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T found)
                yield return found;

            foreach (var childOfChild in FindChildren<T>(child))
            {
                yield return childOfChild;
            }
        }
    }

    /// <summary>
    /// Finds a child element by name in the visual tree.
    /// </summary>
    protected static FrameworkElement? FindChildByName(DependencyObject parent, string name)
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is FrameworkElement element && element.Name == name)
                return element;

            var childOfChild = FindChildByName(child, name);
            if (childOfChild != null)
                return childOfChild;
        }

        return null;
    }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Don't shutdown Application here - it's shared across tests
        // It will be cleaned up by the AppDomain when all tests finish
    }

    /// <summary>
    /// Cleanup method to call after all tests in a collection complete.
    /// </summary>
    public static void CleanupApplication()
    {
        lock (_applicationLock)
        {
            if (_sharedApplication != null)
            {
                try
                {
                    _sharedApplication.Shutdown();
                }
                catch
                {
                    // Ignore shutdown errors
                }
                _sharedApplication = null;
            }
        }
    }
}

