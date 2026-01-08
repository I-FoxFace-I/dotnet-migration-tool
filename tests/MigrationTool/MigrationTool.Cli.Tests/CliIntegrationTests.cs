using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using MigrationTool.Tests.Infrastructure.Fixtures;
using MigrationTool.Tests.Infrastructure.Generators;
using Xunit;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Integration tests for the CLI tool.
/// Uses FakeSolutionGenerator for consistent test data.
/// </summary>
public class CliIntegrationTests : IDisposable
{
    private readonly TempDirectoryFixture _tempDir;
    private readonly string _cliProjectPath;
    
    public CliIntegrationTests()
    {
        _tempDir = new TempDirectoryFixture();
        
        // Find the CLI project path - navigate from test output to source
        // Test runs from: tools/tests/MigrationTool/MigrationTool.Cli.Tests/bin/Debug/net9.0
        // CLI is at: tools/src/MigrationTool/MigrationTool.Cli
        var testDir = AppContext.BaseDirectory;
        _cliProjectPath = Path.GetFullPath(Path.Combine(
            testDir, 
            "..", "..", "..", // bin/Debug/net9.0
            "..", "..", // tests/MigrationTool/MigrationTool.Cli.Tests
            "..", "src", "MigrationTool", "MigrationTool.Cli"
        ));
    }
    
    public void Dispose()
    {
        _tempDir.Dispose();
    }
    
    [Fact]
    public async Task Help_ShowsUsageInformation()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("--help");
        
        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("MigrationTool CLI");
        output.Should().Contain("analyze-solution");
        output.Should().Contain("update-namespace");
        output.Should().Contain("update-project-refs");
        output.Should().Contain("find-usages");
        output.Should().Contain("copy-folder");
    }
    
    [Fact]
    public async Task UpdateNamespace_ValidFile_UpdatesNamespace()
    {
        // Arrange - use TempDirectoryFixture
        var testFile = _tempDir.CreateFile("TestClass.cs", """
            namespace OldNamespace;

            public class TestClass
            {
                public void DoSomething() { }
            }
            """);
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "update-namespace",
            "--file", testFile,
            "--old", "OldNamespace",
            "--new", "NewNamespace"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("Changes").GetInt32().Should().Be(1);
        
        var updatedContent = await File.ReadAllTextAsync(testFile);
        updatedContent.Should().Contain("namespace NewNamespace;");
        updatedContent.Should().NotContain("namespace OldNamespace;");
    }
    
    [Fact]
    public async Task UpdateNamespace_WithUsings_UpdatesAll()
    {
        // Arrange - file with multiple usings
        var testFile = _tempDir.CreateFile("TestWithUsings.cs", """
            using Wpf.Scopes.Tests;
            using Wpf.Scopes.Tests.Core;
            using System.Linq;

            namespace Wpf.Scopes.Tests;

            public class MyTests { }
            """);
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "update-namespace",
            "--file", testFile,
            "--old", "Wpf.Scopes.Tests",
            "--new", "Wpf.Scopes.Unit.Tests"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("Changes").GetInt32().Should().BeGreaterThan(0);
        
        var updatedContent = await File.ReadAllTextAsync(testFile);
        updatedContent.Should().Contain("using Wpf.Scopes.Unit.Tests;");
        updatedContent.Should().Contain("namespace Wpf.Scopes.Unit.Tests;");
        updatedContent.Should().Contain("using System.Linq;"); // Unchanged
    }
    
    [Fact]
    public async Task UpdateNamespace_NoMatch_ReportsNoChanges()
    {
        // Arrange
        var testFile = _tempDir.CreateFile("DifferentNamespace.cs", """
            namespace DifferentNamespace;

            public class TestClass { }
            """);
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "update-namespace",
            "--file", testFile,
            "--old", "OldNamespace",
            "--new", "NewNamespace"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("Changes").GetInt32().Should().Be(0);
    }
    
    [Fact]
    public async Task UpdateProjectRefs_ValidProject_UpdatesReference()
    {
        // Arrange - use FakeSolutionGenerator pattern
        var solution = FakeSolutionGenerator.CreateSolutionWithTests(_tempDir, "RefTest");
        
        // Get the test project and modify its reference
        var testProjectPath = solution.Projects.First(p => p.IsTestProject).Path;
        
        // Act - update the reference path
        var (exitCode, output, error) = await RunCliAsync(
            "update-project-refs",
            "--project", testProjectPath,
            "--old-ref", @"..\RefTest.Core\RefTest.Core.csproj",
            "--new-ref", @"..\src\RefTest.Core\RefTest.Core.csproj"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        
        var updatedContent = await File.ReadAllTextAsync(testProjectPath);
        updatedContent.Should().Contain(@"..\src\RefTest.Core\RefTest.Core.csproj");
        updatedContent.Should().NotContain(@"..\RefTest.Core\RefTest.Core.csproj");
    }
    
    [Fact]
    public async Task UpdateNamespace_NonExistentFile_ReturnsError()
    {
        // Act
        var (exitCode, _, error) = await RunCliAsync(
            "update-namespace",
            "--file", _tempDir.GetPath("NonExistent.cs"),
            "--old", "Old",
            "--new", "New"
        );
        
        // Assert
        exitCode.Should().Be(1);
        error.Should().Contain("Error");
    }
    
    [Fact]
    public async Task UpdateNamespace_GeneratedSolution_UpdatesCorrectly()
    {
        // Arrange - create a full solution structure
        var solution = FakeSolutionGenerator.CreateMinimalSolution(_tempDir, "TestApp");
        
        // Find the generated C# file
        var csFile = Directory.GetFiles(_tempDir.Path, "*.cs", SearchOption.AllDirectories).First();
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "update-namespace",
            "--file", csFile,
            "--old", "TestApp.Core",
            "--new", "TestApp.Domain"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var updatedContent = await File.ReadAllTextAsync(csFile);
        updatedContent.Should().Contain("namespace TestApp.Domain");
    }
    
    [Fact]
    public async Task MoveFolder_DryRun_ListsFilesWithoutMoving()
    {
        // Arrange - create a folder with files
        var sourceFolder = _tempDir.CreateSubdirectory("SourceFolder");
        _tempDir.CreateFile(Path.Combine("SourceFolder", "File1.cs"), """
            namespace SourceFolder;
            public class File1 { }
            """);
        _tempDir.CreateFile(Path.Combine("SourceFolder", "File2.cs"), """
            namespace SourceFolder;
            public class File2 { }
            """);
        var targetFolder = _tempDir.GetPath("TargetFolder");
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "move-folder",
            "--source", sourceFolder,
            "--target", targetFolder,
            "--dry-run"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("DryRun").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(2);
        
        // Source should still exist (dry run)
        Directory.Exists(sourceFolder).Should().BeTrue();
        Directory.Exists(targetFolder).Should().BeFalse();
    }
    
    [Fact]
    public async Task MoveFolder_ActualMove_MovesFilesAndUpdatesNamespaces()
    {
        // Arrange - create a project structure
        var projectDir = _tempDir.CreateSubdirectory("MyProject");
        _tempDir.CreateFile(Path.Combine("MyProject", "MyProject.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        
        var sourceFolder = _tempDir.CreateSubdirectory(Path.Combine("MyProject", "OldFolder"));
        _tempDir.CreateFile(Path.Combine("MyProject", "OldFolder", "MyClass.cs"), """
            namespace MyProject.OldFolder;
            
            public class MyClass 
            { 
                public void DoWork() { }
            }
            """);
        
        var targetFolder = _tempDir.GetPath(Path.Combine("MyProject", "NewFolder"));
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "move-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(1);
        
        // Source should not exist anymore
        Directory.Exists(sourceFolder).Should().BeFalse();
        
        // Target should exist with the file
        Directory.Exists(targetFolder).Should().BeTrue();
        var movedFile = Path.Combine(targetFolder, "MyClass.cs");
        File.Exists(movedFile).Should().BeTrue();
        
        // Check namespace was updated
        var content = await File.ReadAllTextAsync(movedFile);
        content.Should().Contain("namespace MyProject.NewFolder");
        content.Should().NotContain("namespace MyProject.OldFolder");
    }
    
    [Fact]
    public async Task MoveFolder_SourceNotExists_ReturnsError()
    {
        // Arrange
        var sourceFolder = _tempDir.GetPath("NonExistentFolder");
        var targetFolder = _tempDir.GetPath("TargetFolder");
        
        // Act
        var (exitCode, _, error) = await RunCliAsync(
            "move-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(1);
        error.Should().Contain("Source folder not found");
    }
    
    [Fact]
    public async Task MoveFolder_TargetExists_ReturnsError()
    {
        // Arrange
        var sourceFolder = _tempDir.CreateSubdirectory("SourceFolder2");
        var targetFolder = _tempDir.CreateSubdirectory("TargetFolder2");
        
        // Act
        var (exitCode, _, error) = await RunCliAsync(
            "move-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(1);
        error.Should().Contain("Target folder already exists");
    }
    
    [Fact]
    public async Task UpdateSolution_ValidPath_UpdatesProjectPath()
    {
        // Arrange - create a solution file
        var solutionContent = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyProject", "src\MyProject\MyProject.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Global
            EndGlobal
            """;
        var solutionFile = _tempDir.CreateFile("Test.sln", solutionContent);
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "update-solution",
            "--solution", solutionFile,
            "--old-path", @"src\MyProject\MyProject.csproj",
            "--new-path", @"lib\MyProject\MyProject.csproj"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("Updated").GetBoolean().Should().BeTrue();
        
        var updatedContent = await File.ReadAllTextAsync(solutionFile);
        updatedContent.Should().Contain(@"lib\MyProject\MyProject.csproj");
        updatedContent.Should().NotContain(@"src\MyProject\MyProject.csproj");
    }
    
    [Fact]
    public async Task UpdateSolution_PathNotFound_ReportsNotUpdated()
    {
        // Arrange
        var solutionContent = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyProject", "src\MyProject\MyProject.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            """;
        var solutionFile = _tempDir.CreateFile("Test.sln", solutionContent);
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "update-solution",
            "--solution", solutionFile,
            "--old-path", @"nonexistent\path.csproj",
            "--new-path", @"new\path.csproj"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("Updated").GetBoolean().Should().BeFalse();
    }
    
    [Fact]
    public async Task Help_ShowsNewCommands()
    {
        // Act
        var (exitCode, output, _) = await RunCliAsync("--help");
        
        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("move-folder");
        output.Should().Contain("update-solution");
        output.Should().Contain("copy-folder");
    }
    
    [Fact]
    public async Task MoveFolder_EfCoreSolution_MovesRepositoriesFolder()
    {
        // Arrange - create a rich EF Core solution
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, "TestApp");
        
        // Find the Infrastructure project and its Repositories folder
        var infraProject = solution.Projects.First(p => p.Name.EndsWith(".Infrastructure"));
        var infraDir = Path.GetDirectoryName(infraProject.Path)!;
        var sourceFolder = Path.Combine(infraDir, "Repositories");
        var targetFolder = Path.Combine(infraDir, "DataAccess");
        
        // Verify source exists with files
        Directory.Exists(sourceFolder).Should().BeTrue("Repositories folder should exist");
        var filesCount = Directory.GetFiles(sourceFolder, "*.cs").Length;
        filesCount.Should().BeGreaterThan(0, "Should have C# files");
        
        // Act - move Repositories to DataAccess
        var (exitCode, output, error) = await RunCliAsync(
            "move-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(filesCount);
        
        // Verify move
        Directory.Exists(sourceFolder).Should().BeFalse("Source should be gone");
        Directory.Exists(targetFolder).Should().BeTrue("Target should exist");
        
        // Verify namespace updates
        var baseRepoFile = Path.Combine(targetFolder, "BaseRepository.cs");
        File.Exists(baseRepoFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(baseRepoFile);
        content.Should().Contain("namespace TestApp.Infrastructure.DataAccess");
        content.Should().NotContain("namespace TestApp.Infrastructure.Repositories");
    }
    
    [Fact]
    public async Task MoveFolder_EfCoreSolution_DryRunPreservesOriginal()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, "DryRunApp");
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(".Domain"));
        var domainDir = Path.GetDirectoryName(domainProject.Path)!;
        var sourceFolder = Path.Combine(domainDir, "Entities");
        var targetFolder = Path.Combine(domainDir, "Models");
        
        var originalFileCount = Directory.GetFiles(sourceFolder, "*.cs").Length;
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "move-folder",
            "--source", sourceFolder,
            "--target", targetFolder,
            "--dry-run"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("DryRun").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(originalFileCount);
        
        // Original should still exist
        Directory.Exists(sourceFolder).Should().BeTrue("Dry run should preserve source");
        Directory.Exists(targetFolder).Should().BeFalse("Dry run should not create target");
    }
    
    [Fact]
    public async Task UpdateProjectRefs_EfCoreSolution_UpdatesReference()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, "RefUpdateApp");
        
        // Find Infrastructure project (references Domain)
        var infraProject = solution.Projects.First(p => p.Name.EndsWith(".Infrastructure"));
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(".Domain"));
        
        // Act - simulate moving Domain project
        var (exitCode, output, error) = await RunCliAsync(
            "update-project-refs",
            "--project", infraProject.Path,
            "--old-ref", $@"..\{domainProject.Name}\{domainProject.Name}.csproj",
            "--new-ref", $@"..\..\core\{domainProject.Name}\{domainProject.Name}.csproj"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        
        var csprojContent = await File.ReadAllTextAsync(infraProject.Path);
        csprojContent.Should().Contain($@"..\..\core\{domainProject.Name}\{domainProject.Name}.csproj");
    }

    private async Task<(int ExitCode, string Output, string Error)> RunCliAsync(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_cliProjectPath}\" -- {string.Join(" ", args.Select(a => $"\"{a}\""))}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return (process.ExitCode, output.Trim(), error.Trim());
    }
}
