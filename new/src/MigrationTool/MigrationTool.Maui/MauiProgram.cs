using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.DependencyInjection;
using MigrationTool.Localization;
using MigrationTool.Maui.Services;
using MigrationTool.Maui.Services.Validation;
using MigrationTool.Maui.ViewModels;
using MigrationTool.Maui.Views;

namespace MigrationTool.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register Core services
        builder.Services.AddMigrationToolCore();

        // Register Localization
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

        // Register Validation
        builder.Services.AddSingleton<ViewModelBindingValidator>();

        // Register AppState (singleton for shared state)
        builder.Services.AddSingleton<AppState>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ExplorerViewModel>();
        builder.Services.AddTransient<PlannerViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ExplorerPage>();
        builder.Services.AddTransient<PlannerPage>();
        builder.Services.AddTransient<SettingsPage>();

        var app = builder.Build();

#if DEBUG
        // Validate ViewModels at startup
        ValidateViewModelsAtStartup(app.Services);
#endif

        return app;
    }

#if DEBUG
    /// <summary>
    /// Validates all ViewModels at application startup (DEBUG only).
    /// </summary>
    private static void ValidateViewModelsAtStartup(IServiceProvider services)
    {
        var validator = services.GetRequiredService<ViewModelBindingValidator>();
        var logger = services.GetRequiredService<ILogger<App>>();

        try
        {
            logger.LogInformation("🔍 Starting ViewModel binding validation...");

            // Auto-validate all ViewModels in the assembly
            validator.ValidateAssembly(typeof(MauiProgram).Assembly);

            var report = validator.GetReport();
            validator.LogReport(report);

            if (report.HasErrors)
            {
                logger.LogWarning("⚠️ ViewModel binding validation found {Count} issues!", report.FailedCount);
            }
            else
            {
                logger.LogInformation("✅ All ViewModel bindings are valid!");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to validate ViewModels");
        }
    }
#endif
}
