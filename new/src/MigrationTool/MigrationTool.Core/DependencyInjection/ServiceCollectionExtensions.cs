using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MigrationTool.Core.Abstractions.Graph;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Analyzers;
using MigrationTool.Core.Graph;
using MigrationTool.Core.Services;

namespace MigrationTool.Core.DependencyInjection;

/// <summary>
/// Extension methods for registering Migration Tool Core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Migration Tool Core services to the service collection.
    /// </summary>
    public static IServiceCollection AddMigrationToolCore(this IServiceCollection services)
    {
        // File system (can be overridden for different platforms)
        services.TryAddSingleton<IFileSystemService, LocalFileSystemService>();

        // Analyzers
        services.AddSingleton<ICodeAnalyzer, CodeAnalyzer>();
        services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
        services.AddSingleton<ISolutionAnalyzer, SolutionAnalyzer>();

        // Migration services
        services.AddSingleton<IMigrationExecutor, MigrationExecutor>();
        services.AddSingleton<IMigrationPlanner, MigrationPlanner>();

        // Graph services
        services.AddSingleton<ISolutionGraphBuilder, SolutionGraphBuilder>();
        services.AddSingleton<IImpactAnalyzer, ImpactAnalyzer>();

        return services;
    }

    /// <summary>
    /// Adds Migration Tool Core services with a custom file system implementation.
    /// </summary>
    public static IServiceCollection AddMigrationToolCore<TFileSystem>(this IServiceCollection services)
        where TFileSystem : class, IFileSystemService
    {
        services.AddSingleton<IFileSystemService, TFileSystem>();

        // Analyzers
        services.AddSingleton<ICodeAnalyzer, CodeAnalyzer>();
        services.AddSingleton<IProjectAnalyzer, ProjectAnalyzer>();
        services.AddSingleton<ISolutionAnalyzer, SolutionAnalyzer>();

        // Migration services
        services.AddSingleton<IMigrationExecutor, MigrationExecutor>();
        services.AddSingleton<IMigrationPlanner, MigrationPlanner>();

        // Graph services
        services.AddSingleton<ISolutionGraphBuilder, SolutionGraphBuilder>();
        services.AddSingleton<IImpactAnalyzer, ImpactAnalyzer>();

        return services;
    }
}
