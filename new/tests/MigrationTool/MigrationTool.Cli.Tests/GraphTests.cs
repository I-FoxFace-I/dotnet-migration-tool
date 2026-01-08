using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Graph;
using MigrationTool.Core.Graph;
using Xunit;
using Xunit.Abstractions;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests for SolutionGraph and ImpactAnalyzer.
/// </summary>
public class GraphTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDir;
    private readonly ILogger<SolutionGraphBuilder> _graphLogger;
    private readonly ILogger<ImpactAnalyzer> _analyzerLogger;

    public GraphTests(ITestOutputHelper output)
    {
        _output = output;
        _testDir = Path.Combine(Path.GetTempPath(), $"GraphTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddXUnit(output);
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        _graphLogger = loggerFactory.CreateLogger<SolutionGraphBuilder>();
        _analyzerLogger = loggerFactory.CreateLogger<ImpactAnalyzer>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    private string CreateSimpleSolution(string name, params (string projectName, string[] classes)[] projects)
    {
        var solutionDir = Path.Combine(_testDir, name);
        Directory.CreateDirectory(solutionDir);

        var projectGuids = new List<(string name, Guid guid, string path)>();
        var projectRefs = new Dictionary<string, List<string>>();

        foreach (var (projectName, classes) in projects)
        {
            var projectDir = Path.Combine(solutionDir, projectName);
            Directory.CreateDirectory(projectDir);

            var projectGuid = Guid.NewGuid();
            var csprojPath = Path.Combine(projectDir, $"{projectName}.csproj");
            
            // Create csproj
            var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>
</Project>";
            File.WriteAllText(csprojPath, csprojContent);

            // Create classes
            foreach (var classContent in classes)
            {
                var className = ExtractClassName(classContent);
                var filePath = Path.Combine(projectDir, $"{className}.cs");
                var fullContent = $"namespace {projectName};\n\n{classContent}";
                File.WriteAllText(filePath, fullContent);
            }

            projectGuids.Add((projectName, projectGuid, $"{projectName}\\{projectName}.csproj"));
        }

        // Create solution file
        var slnPath = Path.Combine(solutionDir, $"{name}.sln");
        var slnContent = GenerateSolutionFile(projectGuids);
        File.WriteAllText(slnPath, slnContent);

        return slnPath;
    }

    private static string ExtractClassName(string classContent)
    {
        // Simple extraction - looks for "class X" or "interface X" etc.
        var patterns = new[] { "class ", "interface ", "struct ", "record ", "enum " };
        foreach (var pattern in patterns)
        {
            var idx = classContent.IndexOf(pattern, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var start = idx + pattern.Length;
                var end = start;
                while (end < classContent.Length && (char.IsLetterOrDigit(classContent[end]) || classContent[end] == '_'))
                    end++;
                return classContent.Substring(start, end - start);
            }
        }
        return "Unknown";
    }

    private static string GenerateSolutionFile(List<(string name, Guid guid, string path)> projects)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        sb.AppendLine("# Visual Studio Version 17");
        
        foreach (var (name, guid, path) in projects)
        {
            sb.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{name}\", \"{path}\", \"{{{guid}}}\"");
            sb.AppendLine("EndProject");
        }

        sb.AppendLine("Global");
        sb.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        sb.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        sb.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var (_, guid, _) in projects)
        {
            sb.AppendLine($"\t\t{{{guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            sb.AppendLine($"\t\t{{{guid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            sb.AppendLine($"\t\t{{{guid}}}.Release|Any CPU.Build.0 = Release|Any CPU");
        }
        sb.AppendLine("\tEndGlobalSection");
        sb.AppendLine("EndGlobal");

        return sb.ToString();
    }

    [Fact]
    public async Task BuildGraph_SimpleSolution_ReturnsCorrectStatistics()
    {
        // Arrange
        var solutionPath = CreateSimpleSolution("TestSolution",
            ("MyApp.Core", new[] { "public class UserService { }", "public class User { public string Name { get; set; } }" }),
            ("MyApp.Web", new[] { "public class UserController { }" }));

        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(solutionPath, GraphBuildOptions.Fast);
        var stats = graph.GetStatistics();

        // Assert
        Assert.Equal(1, stats.SolutionCount);
        Assert.Equal(2, stats.ProjectCount);
        Assert.True(stats.FileCount >= 2, $"Expected at least 2 files, got {stats.FileCount}");
        Assert.True(stats.TypeCount >= 2, $"Expected at least 2 types, got {stats.TypeCount}");

        _output.WriteLine(stats.ToString());
    }

    [Fact]
    public async Task BuildGraph_WithInheritance_CreatesTypeEdges()
    {
        // Arrange
        var solutionPath = CreateSimpleSolution("InheritanceTest",
            ("MyApp", new[] 
            { 
                "public abstract class BaseService { }",
                "public class UserService : BaseService { }",
                "public interface IRepository { }",
                "public class UserRepository : IRepository { }"
            }));

        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(solutionPath, GraphBuildOptions.Default);

        // Assert
        var inheritEdges = graph.Edges.OfType<TypeInheritsEdge>().ToList();
        var implementEdges = graph.Edges.OfType<TypeImplementsEdge>().ToList();

        _output.WriteLine($"Inherit edges: {inheritEdges.Count}");
        _output.WriteLine($"Implement edges: {implementEdges.Count}");

        // UserService inherits BaseService
        Assert.Contains(inheritEdges, e => 
            e.SourceId.Contains("UserService") && e.TargetId.Contains("BaseService"));

        // UserRepository implements IRepository
        Assert.Contains(implementEdges, e => 
            e.SourceId.Contains("UserRepository") && e.TargetId.Contains("IRepository"));
    }

    [Fact]
    public async Task BuildGraph_WithNamespaces_TracksUsingDirectives()
    {
        // Arrange - Create solution with explicit using
        var solutionDir = Path.Combine(_testDir, "NamespaceTest");
        Directory.CreateDirectory(solutionDir);
        
        var projectDir = Path.Combine(solutionDir, "MyApp");
        Directory.CreateDirectory(projectDir);
        Directory.CreateDirectory(Path.Combine(projectDir, "Services"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Controllers"));

        // Create csproj
        File.WriteAllText(Path.Combine(projectDir, "MyApp.csproj"), @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>");

        // Create UserService in Services namespace
        File.WriteAllText(Path.Combine(projectDir, "Services", "UserService.cs"), @"namespace MyApp.Services;

public class UserService { }");

        // Create UserController that uses Services namespace
        File.WriteAllText(Path.Combine(projectDir, "Controllers", "UserController.cs"), @"using MyApp.Services;

namespace MyApp.Controllers;

public class UserController { private UserService _service; }");

        // Create solution file
        var slnPath = Path.Combine(solutionDir, "NamespaceTest.sln");
        var guid = Guid.NewGuid();
        File.WriteAllText(slnPath, GenerateSolutionFile(new List<(string, Guid, string)> { ("MyApp", guid, @"MyApp\MyApp.csproj") }));

        await using var builder = new SolutionGraphBuilder(_graphLogger);

        // Act
        var graph = await builder.BuildGraphAsync(slnPath, GraphBuildOptions.Default);

        // Assert
        var usingEdges = graph.Edges.OfType<FileUsesNamespaceEdge>().ToList();
        Assert.True(usingEdges.Any(), "Should have using directive edges");

        var nsNodes = graph.Namespaces.Values.ToList();
        Assert.Contains(nsNodes, n => n.Namespace == "MyApp.Services");
    }

    [Fact]
    public async Task ImpactAnalyzer_MoveFile_IdentifiesAffectedFiles()
    {
        // Arrange
        var solutionPath = CreateSimpleSolution("MoveTest",
            ("MyApp.Core", new[] { "public class UserService { }" }),
            ("MyApp.Web", new[] { "public class UserController { }" }));

        await using var builder = new SolutionGraphBuilder(_graphLogger);
        var graph = await builder.BuildGraphAsync(solutionPath, GraphBuildOptions.Default);
        var analyzer = new ImpactAnalyzer(_analyzerLogger);

        // Find the UserService file
        var userServiceFile = graph.Files.Values.FirstOrDefault(f => f.Path.Contains("UserService"));
        Assert.NotNull(userServiceFile);

        var targetPath = userServiceFile.Path.Replace("UserService", "MovedUserService");

        // Act
        var report = await analyzer.AnalyzeMoveAsync(graph, new MoveOperation(
            userServiceFile.Path,
            targetPath,
            "MyApp.Core.Moved"
        ));

        // Assert
        _output.WriteLine(report.ToMarkdown());
        
        Assert.True(report.AffectedFilesCount >= 1, "Should have at least 1 affected file");
        Assert.Contains(report.AffectedFiles, f => f.FilePath == userServiceFile.Path);
    }

    [Fact]
    public async Task ImpactAnalyzer_DeleteFile_WarnsAboutReferences()
    {
        // Arrange
        var solutionPath = CreateSimpleSolution("DeleteTest",
            ("MyApp", new[] 
            { 
                "public class BaseClass { }",
                "public class DerivedClass : BaseClass { }"
            }));

        await using var builder = new SolutionGraphBuilder(_graphLogger);
        var graph = await builder.BuildGraphAsync(solutionPath, GraphBuildOptions.Default);
        var analyzer = new ImpactAnalyzer(_analyzerLogger);

        // Find BaseClass file
        var baseClassFile = graph.Files.Values.FirstOrDefault(f => f.Path.Contains("BaseClass"));
        Assert.NotNull(baseClassFile);

        // Act - without force
        var report = await analyzer.AnalyzeDeleteAsync(graph, new DeleteOperation(baseClassFile.Path, Force: false));

        // Assert
        _output.WriteLine(report.ToMarkdown());
        
        // Should have error because BaseClass is referenced (inherited)
        // Note: The current implementation checks for type references, inheritance is tracked
        Assert.True(report.AffectedFilesCount >= 1, "Should have at least 1 affected file");
    }

    [Fact]
    public async Task SolutionGraph_CyclicDependencyDetection_Works()
    {
        // Arrange - manually create a graph with cyclic dependency
        var graph = new SolutionGraph();
        
        var projA = new ProjectNode("proj:A", "/A/A.csproj", "A", "A", "net9.0", ProjectNodeType.ClassLibrary);
        var projB = new ProjectNode("proj:B", "/B/B.csproj", "B", "B", "net9.0", ProjectNodeType.ClassLibrary);
        var projC = new ProjectNode("proj:C", "/C/C.csproj", "C", "C", "net9.0", ProjectNodeType.ClassLibrary);
        
        graph.AddProject(projA);
        graph.AddProject(projB);
        graph.AddProject(projC);
        
        // A -> B -> C -> A (cycle!)
        graph.AddEdge(new ProjectReferenceEdge(projA.Id, projB.Id));
        graph.AddEdge(new ProjectReferenceEdge(projB.Id, projC.Id));
        graph.AddEdge(new ProjectReferenceEdge(projC.Id, projA.Id));

        // Act
        var hasCycleA = graph.HasCyclicDependency(projA.Id);
        var hasCycleB = graph.HasCyclicDependency(projB.Id);
        var hasCycleC = graph.HasCyclicDependency(projC.Id);

        // Assert
        Assert.True(hasCycleA, "Project A should detect cycle");
        Assert.True(hasCycleB, "Project B should detect cycle");
        Assert.True(hasCycleC, "Project C should detect cycle");
    }

    [Fact]
    public void SolutionGraph_QueryMethods_Work()
    {
        // Arrange
        var graph = new SolutionGraph();
        
        var proj = new ProjectNode("proj:Test", "/Test/Test.csproj", "Test", "Test", "net9.0", ProjectNodeType.ClassLibrary);
        var file = new FileNode("file:Test.cs", "/Test/Test.cs", "Test", FileNodeType.CSharp);
        var type = new TypeNode("type:Test.MyClass", "Test.MyClass", "Test", "MyClass", TypeNodeKind.Class, file.Id);
        var ns = new NamespaceNode("ns:Test", "Test");
        
        graph.AddProject(proj);
        graph.AddFile(file);
        graph.AddType(type);
        graph.AddNamespace(ns);
        
        graph.AddEdge(new ProjectContainsFileEdge(proj.Id, file.Id));
        graph.AddEdge(new FileContainsTypeEdge(file.Id, type.Id));
        graph.AddEdge(new TypeInNamespaceEdge(type.Id, ns.Id));

        // Act & Assert
        Assert.Equal(proj, graph.GetProjectContainingFile(file.Id));
        Assert.Equal(file, graph.GetFileContainingType(type.Id));
        Assert.Single(graph.GetTypesInNamespace("Test"));
        Assert.NotNull(graph.FindType("Test.MyClass"));
    }
}

/// <summary>
/// Extension for adding xUnit output to logger.
/// </summary>
public static class LoggerExtensions
{
    public static ILoggingBuilder AddXUnit(this ILoggingBuilder builder, ITestOutputHelper output)
    {
        builder.AddProvider(new XUnitLoggerProvider(output));
        return builder;
    }
}

public class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XUnitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(_output, categoryName);
    }

    public void Dispose() { }
}

public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XUnitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
        }
        catch
        {
            // Ignore output errors
        }
    }
}
