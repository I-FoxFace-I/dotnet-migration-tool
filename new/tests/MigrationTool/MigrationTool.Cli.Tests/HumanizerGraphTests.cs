using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Graph;
using MigrationTool.Core.Graph;
using MigrationTool.Tests.Infrastructure.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests for SolutionGraph using the real Humanizer project.
/// These tests use a real-world open source project to validate graph building.
/// </summary>
public class HumanizerGraphTests : IClassFixture<HumanizerFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly TestProjectFixture _humanizer;
    private readonly ILogger<SolutionGraphBuilder> _graphLogger;
    private readonly ILogger<ImpactAnalyzer> _analyzerLogger;

    public HumanizerGraphTests(HumanizerFixture fixture, ITestOutputHelper output)
    {
        _output = output;
        _humanizer = fixture.Fixture;

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new XUnitLoggerProvider(output));
            builder.SetMinimumLevel(LogLevel.Information);
        });
        _graphLogger = loggerFactory.CreateLogger<SolutionGraphBuilder>();
        _analyzerLogger = loggerFactory.CreateLogger<ImpactAnalyzer>();
    }

    [Fact]
    public void Fixture_ShouldHaveEntryPoint()
    {
        // Assert - Humanizer uses .csproj as entry point (no .sln file)
        Assert.NotNull(_humanizer.EntryPoint);
        Assert.True(File.Exists(_humanizer.EntryPoint), 
            $"Entry point should exist at: {_humanizer.EntryPoint}");
        
        _output.WriteLine($"Entry point: {_humanizer.EntryPoint}");
        _output.WriteLine($"Project root: {_humanizer.ProjectRoot}");
        _output.WriteLine($"Has solution: {_humanizer.HasSolution}");
        _output.WriteLine($"Has main project: {_humanizer.HasMainProject}");
    }

    [Fact]
    public void Fixture_ShouldHaveCSharpFiles()
    {
        // Act
        var csFiles = _humanizer.GetCSharpFiles().ToList();

        // Assert
        Assert.NotEmpty(csFiles);
        _output.WriteLine($"Found {csFiles.Count} C# files");
        
        // Log first 10 files
        foreach (var file in csFiles.Take(10))
        {
            _output.WriteLine($"  - {Path.GetRelativePath(_humanizer.ProjectRoot, file)}");
        }
    }

    [Fact]
    public void Fixture_ShouldHaveProjectFiles()
    {
        // Act
        var projects = _humanizer.GetProjectFiles().ToList();

        // Assert
        Assert.NotEmpty(projects);
        _output.WriteLine($"Found {projects.Count} project files:");
        
        foreach (var proj in projects)
        {
            _output.WriteLine($"  - {Path.GetRelativePath(_humanizer.ProjectRoot, proj)}");
        }
    }

    [Fact]
    public async Task BuildGraph_Humanizer_ReturnsValidGraph()
    {
        // Arrange
        Assert.NotNull(_humanizer.EntryPoint);
        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(_humanizer.EntryPoint, GraphBuildOptions.Fast);
        var stats = graph.GetStatistics();

        // Assert
        Assert.True(stats.ProjectCount >= 1, "Should have at least 1 project");
        Assert.True(stats.FileCount >= 10, $"Should have at least 10 files, got {stats.FileCount}");
        Assert.True(stats.TypeCount >= 10, $"Should have at least 10 types, got {stats.TypeCount}");

        _output.WriteLine(stats.ToString());
    }

    [Fact]
    public async Task BuildGraph_Humanizer_DetectsNamespaces()
    {
        // Arrange
        Assert.NotNull(_humanizer.EntryPoint);
        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(_humanizer.EntryPoint, GraphBuildOptions.Default);

        // Assert
        var namespaces = graph.Namespaces.Values.ToList();
        Assert.NotEmpty(namespaces);
        
        _output.WriteLine($"Found {namespaces.Count} namespaces:");
        foreach (var ns in namespaces.Take(20))
        {
            _output.WriteLine($"  - {ns.Namespace}");
        }

        // Humanizer should have Humanizer namespace
        Assert.Contains(namespaces, n => n.Namespace.StartsWith("Humanizer"));
    }

    [Fact]
    public async Task BuildGraph_Humanizer_DetectsProjectReferences()
    {
        // Arrange
        Assert.NotNull(_humanizer.EntryPoint);
        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(_humanizer.EntryPoint, GraphBuildOptions.Fast);

        // Assert - Humanizer main project has no project references (it's the core library)
        var projectRefEdges = graph.Edges.OfType<ProjectReferenceEdge>().ToList();
        
        _output.WriteLine($"Found {projectRefEdges.Count} project references:");
        foreach (var edge in projectRefEdges.Take(10))
        {
            var source = graph.Projects.GetValueOrDefault(edge.SourceId);
            var target = graph.Projects.GetValueOrDefault(edge.TargetId);
            _output.WriteLine($"  - {source?.Name ?? edge.SourceId} -> {target?.Name ?? edge.TargetId}");
        }
    }

    [Fact]
    public async Task BuildGraph_Humanizer_DetectsInheritance()
    {
        // Arrange
        Assert.NotNull(_humanizer.EntryPoint);
        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(_humanizer.EntryPoint, GraphBuildOptions.Default);

        // Assert
        var inheritEdges = graph.Edges.OfType<TypeInheritsEdge>().ToList();
        var implementEdges = graph.Edges.OfType<TypeImplementsEdge>().ToList();
        
        _output.WriteLine($"Found {inheritEdges.Count} inheritance relationships");
        _output.WriteLine($"Found {implementEdges.Count} interface implementations");

        // Log some examples
        _output.WriteLine("\nSample inheritance:");
        foreach (var edge in inheritEdges.Take(5))
        {
            var source = graph.Types.GetValueOrDefault(edge.SourceId);
            _output.WriteLine($"  - {source?.Name ?? edge.SourceId} : {edge.TargetId.Replace("type:", "")}");
        }
    }

    [Fact]
    public async Task ImpactAnalyzer_Humanizer_AnalyzeMoveType()
    {
        // Arrange
        Assert.NotNull(_humanizer.EntryPoint);
        await using var builder = new SolutionGraphBuilder(_graphLogger);
        var graph = await builder.BuildGraphAsync(_humanizer.EntryPoint, GraphBuildOptions.Default);
        var analyzer = new ImpactAnalyzer(_analyzerLogger);

        // Find a type to analyze
        var sampleType = graph.Types.Values
            .FirstOrDefault(t => t.IsPublic && !t.Name.Contains("Test"));

        if (sampleType == null)
        {
            _output.WriteLine("No suitable type found for analysis");
            return;
        }

        _output.WriteLine($"Analyzing move of: {sampleType.FullName}");

        // Act
        var report = await analyzer.AnalyzeMoveTypeAsync(graph, 
            new MoveTypeOperation(sampleType.FullName, "Humanizer.Moved"));

        // Assert
        _output.WriteLine(report.ToMarkdown());
        
        Assert.NotNull(report);
        Assert.True(report.AffectedFilesCount >= 1, "Moving a type should affect at least 1 file");
    }

    [Fact]
    public async Task ImpactAnalyzer_Humanizer_AnalyzeRenameNamespace()
    {
        // Arrange
        Assert.NotNull(_humanizer.EntryPoint);
        await using var builder = new SolutionGraphBuilder(_graphLogger);
        var graph = await builder.BuildGraphAsync(_humanizer.EntryPoint, GraphBuildOptions.Default);
        var analyzer = new ImpactAnalyzer(_analyzerLogger);

        // Find a namespace to analyze
        var sampleNamespace = graph.Namespaces.Values
            .FirstOrDefault(n => n.Namespace.StartsWith("Humanizer") && 
                                 !n.Namespace.Contains("Test"));

        if (sampleNamespace == null)
        {
            _output.WriteLine("No suitable namespace found for analysis");
            return;
        }

        _output.WriteLine($"Analyzing rename of namespace: {sampleNamespace.Namespace}");

        // Act
        var report = await analyzer.AnalyzeRenameNamespaceAsync(graph, 
            new RenameNamespaceOperation(sampleNamespace.Namespace, "Humanizer.Renamed"));

        // Assert
        _output.WriteLine(report.ToMarkdown());
        
        Assert.NotNull(report);
        // A namespace rename typically affects multiple files
    }
}
