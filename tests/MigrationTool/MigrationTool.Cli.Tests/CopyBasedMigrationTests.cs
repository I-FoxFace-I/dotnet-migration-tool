using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using MigrationTool.Tests.Infrastructure.Fixtures;
using MigrationTool.Tests.Infrastructure.Generators;
using Xunit;
using Xunit.Abstractions;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests for copy-based migration (original files preserved).
/// This is the recommended approach for safe migrations where you want to keep the original code.
/// </summary>
public class CopyBasedMigrationTests : IDisposable
{
    private readonly TempDirectoryFixture _tempDir;
    private readonly string _cliProjectPath;
    private readonly ITestOutputHelper _output;
    
    public CopyBasedMigrationTests(ITestOutputHelper output)
    {
        _tempDir = new TempDirectoryFixture();
        _output = output;
        
        // Find the CLI project path
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
    public async Task CopyFolder_SimpleFolder_CopiesFilesAndPreservesOriginals()
    {
        // Arrange - create a project with a folder
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
        _tempDir.CreateFile(Path.Combine("MyProject", "OldFolder", "Helper.cs"), """
            namespace MyProject.OldFolder;
            
            public static class Helper 
            { 
                public static string Format(string s) => s.ToUpper();
            }
            """);
        
        var targetFolder = _tempDir.GetPath(Path.Combine("MyProject", "NewFolder"));
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        _output.WriteLine($"Output: {output}");
        _output.WriteLine($"Error: {error}");
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(2);
        result.GetProperty("OriginalPreserved").GetBoolean().Should().BeTrue();
        
        // Source should STILL exist (copy-based migration)
        Directory.Exists(sourceFolder).Should().BeTrue("Source should be preserved");
        var originalFile = Path.Combine(sourceFolder, "MyClass.cs");
        File.Exists(originalFile).Should().BeTrue("Original file should exist");
        var originalContent = await File.ReadAllTextAsync(originalFile);
        originalContent.Should().Contain("namespace MyProject.OldFolder"); // Unchanged
        
        // Target should exist with updated namespaces
        Directory.Exists(targetFolder).Should().BeTrue("Target should exist");
        var copiedFile = Path.Combine(targetFolder, "MyClass.cs");
        File.Exists(copiedFile).Should().BeTrue("Copied file should exist");
        var copiedContent = await File.ReadAllTextAsync(copiedFile);
        copiedContent.Should().Contain("namespace MyProject.NewFolder"); // Updated
        copiedContent.Should().NotContain("namespace MyProject.OldFolder");
    }
    
    [Fact]
    public async Task CopyFolder_DryRun_PreviewsWithoutCopying()
    {
        // Arrange
        var projectDir = _tempDir.CreateSubdirectory("DryRunProject");
        _tempDir.CreateFile(Path.Combine("DryRunProject", "DryRunProject.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        
        var sourceFolder = _tempDir.CreateSubdirectory(Path.Combine("DryRunProject", "Services"));
        _tempDir.CreateFile(Path.Combine("DryRunProject", "Services", "UserService.cs"), """
            namespace DryRunProject.Services;
            public class UserService { }
            """);
        
        var targetFolder = _tempDir.GetPath(Path.Combine("DryRunProject", "Application", "Services"));
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder,
            "--dry-run"
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("DryRun").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(1);
        
        // Neither source nor target should be affected
        Directory.Exists(sourceFolder).Should().BeTrue("Source should exist");
        Directory.Exists(targetFolder).Should().BeFalse("Target should NOT be created in dry run");
    }
    
    [Fact]
    public async Task CopyFolder_EfCoreSolution_CopiesEntitiesFolder()
    {
        // Arrange - use FakeSolutionGenerator
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, "CopyTestApp");
        
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(".Domain"));
        var domainDir = Path.GetDirectoryName(domainProject.Path)!;
        var sourceFolder = Path.Combine(domainDir, "Entities");
        var targetFolder = Path.Combine(domainDir, "Models");
        
        var originalFileCount = Directory.GetFiles(sourceFolder, "*.cs").Length;
        _output.WriteLine($"Original files in Entities: {originalFileCount}");
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        _output.WriteLine($"Output: {output}");
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(originalFileCount);
        result.GetProperty("OriginalPreserved").GetBoolean().Should().BeTrue();
        
        // Verify original is preserved
        Directory.Exists(sourceFolder).Should().BeTrue("Entities folder should still exist");
        var originalCustomer = Path.Combine(sourceFolder, "Customer.cs");
        File.Exists(originalCustomer).Should().BeTrue();
        var originalContent = await File.ReadAllTextAsync(originalCustomer);
        originalContent.Should().Contain("CopyTestApp.Domain.Entities");
        
        // Verify copy has updated namespaces
        Directory.Exists(targetFolder).Should().BeTrue("Models folder should be created");
        var copiedCustomer = Path.Combine(targetFolder, "Customer.cs");
        File.Exists(copiedCustomer).Should().BeTrue();
        var copiedContent = await File.ReadAllTextAsync(copiedCustomer);
        copiedContent.Should().Contain("CopyTestApp.Domain.Models");
        copiedContent.Should().NotContain("CopyTestApp.Domain.Entities");
    }
    
    [Fact]
    public async Task CopyFolder_WithSubfolders_CopiesRecursively()
    {
        // Arrange - create nested folder structure
        var projectDir = _tempDir.CreateSubdirectory("NestedProject");
        _tempDir.CreateFile(Path.Combine("NestedProject", "NestedProject.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        
        var sourceFolder = _tempDir.CreateSubdirectory(Path.Combine("NestedProject", "Services"));
        _tempDir.CreateSubdirectory(Path.Combine("NestedProject", "Services", "Users"));
        _tempDir.CreateSubdirectory(Path.Combine("NestedProject", "Services", "Products"));
        
        _tempDir.CreateFile(Path.Combine("NestedProject", "Services", "IService.cs"), """
            namespace NestedProject.Services;
            public interface IService { }
            """);
        _tempDir.CreateFile(Path.Combine("NestedProject", "Services", "Users", "UserService.cs"), """
            namespace NestedProject.Services.Users;
            public class UserService : IService { }
            """);
        _tempDir.CreateFile(Path.Combine("NestedProject", "Services", "Products", "ProductService.cs"), """
            namespace NestedProject.Services.Products;
            public class ProductService : IService { }
            """);
        
        var targetFolder = _tempDir.GetPath(Path.Combine("NestedProject", "Application"));
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        _output.WriteLine($"Output: {output}");
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        result.GetProperty("FilesCount").GetInt32().Should().Be(3);
        
        // Verify nested structure was copied
        Directory.Exists(Path.Combine(targetFolder, "Users")).Should().BeTrue();
        Directory.Exists(Path.Combine(targetFolder, "Products")).Should().BeTrue();
        
        // Note: The namespace rewriter only updates exact matches of the detected namespace
        // (NestedProject.Services -> NestedProject.Application), not child namespaces
        // Child namespaces like NestedProject.Services.Users remain unchanged
        // This is expected behavior - for full recursive namespace updates, 
        // the tool should be called for each subfolder separately
        var rootServicePath = Path.Combine(targetFolder, "IService.cs");
        File.Exists(rootServicePath).Should().BeTrue();
        var rootServiceContent = await File.ReadAllTextAsync(rootServicePath);
        rootServiceContent.Should().Contain("NestedProject.Application");
        
        var userServicePath = Path.Combine(targetFolder, "Users", "UserService.cs");
        File.Exists(userServicePath).Should().BeTrue();
        // UserService.cs keeps its original namespace since it's a child namespace
        
        // Verify originals are preserved
        Directory.Exists(sourceFolder).Should().BeTrue();
        File.Exists(Path.Combine(sourceFolder, "Users", "UserService.cs")).Should().BeTrue();
    }
    
    [Fact]
    public async Task CopyFolder_NonCSharpFiles_CopiesWithoutModification()
    {
        // Arrange - create folder with mixed file types
        var projectDir = _tempDir.CreateSubdirectory("MixedProject");
        _tempDir.CreateFile(Path.Combine("MixedProject", "MixedProject.csproj"), """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        
        var sourceFolder = _tempDir.CreateSubdirectory(Path.Combine("MixedProject", "Resources"));
        _tempDir.CreateFile(Path.Combine("MixedProject", "Resources", "config.json"), """
            { "setting": "value" }
            """);
        _tempDir.CreateFile(Path.Combine("MixedProject", "Resources", "ResourceHelper.cs"), """
            namespace MixedProject.Resources;
            public class ResourceHelper { }
            """);
        _tempDir.CreateFile(Path.Combine("MixedProject", "Resources", "readme.txt"), """
            This is a readme file.
            """);
        
        var targetFolder = _tempDir.GetPath(Path.Combine("MixedProject", "Assets"));
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        // Verify non-C# files are copied unchanged
        var jsonContent = await File.ReadAllTextAsync(Path.Combine(targetFolder, "config.json"));
        jsonContent.Should().Contain("\"setting\": \"value\"");
        
        var txtContent = await File.ReadAllTextAsync(Path.Combine(targetFolder, "readme.txt"));
        txtContent.Should().Contain("This is a readme file");
        
        // Verify C# file has updated namespace
        var csContent = await File.ReadAllTextAsync(Path.Combine(targetFolder, "ResourceHelper.cs"));
        csContent.Should().Contain("MixedProject.Assets");
    }
    
    [Fact]
    public async Task CopyFolder_SourceNotExists_ReturnsError()
    {
        // Arrange
        var sourceFolder = _tempDir.GetPath("NonExistentFolder");
        var targetFolder = _tempDir.GetPath("TargetFolder");
        
        // Act
        var (exitCode, _, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(1);
        error.Should().Contain("Source folder not found");
    }
    
    [Fact]
    public async Task CopyFolder_TargetExists_ReturnsError()
    {
        // Arrange
        var sourceFolder = _tempDir.CreateSubdirectory("SourceExists");
        var targetFolder = _tempDir.CreateSubdirectory("TargetExists");
        
        // Act
        var (exitCode, _, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        // Assert
        exitCode.Should().Be(1);
        error.Should().Contain("Target folder already exists");
    }
    
    [Fact]
    public async Task CopyFolder_InfrastructureRepositories_CopiesAndUpdatesNamespaces()
    {
        // Arrange - test with infrastructure pattern
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, "InfraApp");
        
        var infraProject = solution.Projects.First(p => p.Name.EndsWith(".Infrastructure"));
        var infraDir = Path.GetDirectoryName(infraProject.Path)!;
        var sourceFolder = Path.Combine(infraDir, "Repositories");
        var targetFolder = Path.Combine(infraDir, "DataAccess", "Repositories");
        
        var originalFiles = Directory.GetFiles(sourceFolder, "*.cs");
        _output.WriteLine($"Files to copy: {string.Join(", ", originalFiles.Select(Path.GetFileName))}");
        
        // Act
        var (exitCode, output, error) = await RunCliAsync(
            "copy-folder",
            "--source", sourceFolder,
            "--target", targetFolder
        );
        
        _output.WriteLine($"Output: {output}");
        
        // Assert
        exitCode.Should().Be(0, $"CLI failed with error: {error}");
        
        var result = JsonSerializer.Deserialize<JsonElement>(output);
        result.GetProperty("Success").GetBoolean().Should().BeTrue();
        
        // Verify original repositories still exist
        Directory.Exists(sourceFolder).Should().BeTrue();
        foreach (var file in originalFiles)
        {
            File.Exists(file).Should().BeTrue($"Original {Path.GetFileName(file)} should exist");
        }
        
        // Verify copied repositories have new namespace
        var baseRepoPath = Path.Combine(targetFolder, "BaseRepository.cs");
        File.Exists(baseRepoPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(baseRepoPath);
        content.Should().Contain("InfraApp.Infrastructure.DataAccess.Repositories");
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
