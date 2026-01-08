using FluentAssertions;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Services;
using MigrationTool.Tests.Infrastructure.Generators;
using Xunit;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests for cross-solution migration functionality.
/// </summary>
public class CrossSolutionMigrationTests : IDisposable
{
    private readonly string _testDir;
    private readonly CrossSolutionMigrationService _service;

    public CrossSolutionMigrationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"CrossSlnTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _service = new CrossSolutionMigrationService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public async Task AnalyzeMigration_ValidPaths_ReturnsAnalysis()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("SourceProject");
        var targetSln = await CreateTestSolutionAsync("TargetProject");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "SourceProject", "Services");
        Directory.CreateDirectory(sourceFolder);
        await File.WriteAllTextAsync(
            Path.Combine(sourceFolder, "MyService.cs"),
            @"namespace SourceProject.Services;
public class MyService { }");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = "TargetProject/Services",
            DryRun = true
        };

        // Act
        var analysis = await _service.AnalyzeMigrationAsync(options);

        // Assert
        analysis.CanMigrate.Should().BeTrue();
        analysis.FilesToMigrate.Should().Be(1);
        analysis.Files.Should().Contain("MyService.cs");
    }

    [Fact]
    public async Task AnalyzeMigration_SourceNotFound_ReturnsError()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("Source");
        var targetSln = await CreateTestSolutionAsync("Target");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = "NonExistent/Path",
            TargetPath = "Target/Path"
        };

        // Act
        var analysis = await _service.AnalyzeMigrationAsync(options);

        // Assert
        analysis.CanMigrate.Should().BeFalse();
        analysis.BlockingError.Should().Contain("not found");
    }

    [Fact]
    public async Task MigrateFolderAsync_DryRun_DoesNotCopyFiles()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("SourceApp");
        var targetSln = await CreateTestSolutionAsync("TargetApp");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "SourceApp", "Utils");
        Directory.CreateDirectory(sourceFolder);
        await File.WriteAllTextAsync(
            Path.Combine(sourceFolder, "Helper.cs"),
            @"namespace SourceApp.Utils;
public class Helper { }");

        var targetFolder = Path.Combine(Path.GetDirectoryName(targetSln)!, "TargetApp", "Utils");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = targetFolder,
            DryRun = true
        };

        // Act
        var result = await _service.MigrateFolderAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.DryRun.Should().BeTrue();
        result.MigratedFilesCount.Should().Be(1);
        Directory.Exists(targetFolder).Should().BeFalse(); // Not actually created
    }

    [Fact]
    public async Task MigrateFolderAsync_CopyMode_PreservesOriginal()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("Original");
        var targetSln = await CreateTestSolutionAsync("Clone");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "Original", "Models");
        Directory.CreateDirectory(sourceFolder);
        var sourceCode = @"namespace Original.Models;
public class User { public string Name { get; set; } }";
        await File.WriteAllTextAsync(Path.Combine(sourceFolder, "User.cs"), sourceCode);

        var targetFolder = Path.Combine(Path.GetDirectoryName(targetSln)!, "Clone", "Models");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = targetFolder,
            PreserveOriginal = true,
            DryRun = false
        };

        // Act
        var result = await _service.MigrateFolderAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.MigratedFilesCount.Should().Be(1);
        
        // Original should still exist
        File.Exists(Path.Combine(sourceFolder, "User.cs")).Should().BeTrue();
        var originalCode = await File.ReadAllTextAsync(Path.Combine(sourceFolder, "User.cs"));
        originalCode.Should().Contain("namespace Original.Models");
        
        // Copy should exist with new namespace
        File.Exists(Path.Combine(targetFolder, "User.cs")).Should().BeTrue();
        var copiedCode = await File.ReadAllTextAsync(Path.Combine(targetFolder, "User.cs"));
        copiedCode.Should().Contain("namespace Clone.Models");
    }

    [Fact]
    public async Task MigrateFolderAsync_MoveMode_DeletesOriginal()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("ToMove");
        var targetSln = await CreateTestSolutionAsync("Destination");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "ToMove", "Services");
        Directory.CreateDirectory(sourceFolder);
        await File.WriteAllTextAsync(
            Path.Combine(sourceFolder, "Service.cs"),
            @"namespace ToMove.Services;
public class Service { }");

        var targetFolder = Path.Combine(Path.GetDirectoryName(targetSln)!, "Destination", "Services");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = targetFolder,
            PreserveOriginal = false, // Move mode
            DryRun = false
        };

        // Act
        var result = await _service.MigrateFolderAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        
        // Original should be deleted
        File.Exists(Path.Combine(sourceFolder, "Service.cs")).Should().BeFalse();
        
        // Target should exist
        File.Exists(Path.Combine(targetFolder, "Service.cs")).Should().BeTrue();
    }

    [Fact]
    public async Task MigrateFolderAsync_WithCustomNamespace_UsesCustomNamespace()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("OldCompany");
        var targetSln = await CreateTestSolutionAsync("NewCompany");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "OldCompany", "Core");
        Directory.CreateDirectory(sourceFolder);
        await File.WriteAllTextAsync(
            Path.Combine(sourceFolder, "Entity.cs"),
            @"namespace OldCompany.Core;
public class Entity { }");

        var targetFolder = Path.Combine(Path.GetDirectoryName(targetSln)!, "NewCompany", "Domain");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = targetFolder,
            OldNamespacePrefix = "OldCompany.Core",
            NewNamespacePrefix = "NewCompany.Domain.Entities",
            PreserveOriginal = true,
            DryRun = false
        };

        // Act
        var result = await _service.MigrateFolderAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        
        var targetCode = await File.ReadAllTextAsync(Path.Combine(targetFolder, "Entity.cs"));
        targetCode.Should().Contain("namespace NewCompany.Domain.Entities");
    }

    [Fact]
    public async Task MigrateFolderAsync_WithExcludePattern_ExcludesFiles()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("WithDesigner");
        var targetSln = await CreateTestSolutionAsync("WithoutDesigner");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "WithDesigner", "Forms");
        Directory.CreateDirectory(sourceFolder);
        await File.WriteAllTextAsync(Path.Combine(sourceFolder, "MyForm.cs"), "namespace WithDesigner.Forms; public class MyForm { }");
        await File.WriteAllTextAsync(Path.Combine(sourceFolder, "MyForm.Designer.cs"), "// Designer file");

        var targetFolder = Path.Combine(Path.GetDirectoryName(targetSln)!, "WithoutDesigner", "Forms");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = targetFolder,
            ExcludePatterns = ["*.Designer.cs"],
            PreserveOriginal = true,
            DryRun = false
        };

        // Act
        var result = await _service.MigrateFolderAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.MigratedFilesCount.Should().Be(1);
        File.Exists(Path.Combine(targetFolder, "MyForm.cs")).Should().BeTrue();
        File.Exists(Path.Combine(targetFolder, "MyForm.Designer.cs")).Should().BeFalse();
    }

    [Fact]
    public async Task MigrateFolderAsync_MultipleFiles_MigratesAll()
    {
        // Arrange
        var sourceSln = await CreateTestSolutionAsync("MultiFile");
        var targetSln = await CreateTestSolutionAsync("MultiFileCopy");
        
        var sourceFolder = Path.Combine(Path.GetDirectoryName(sourceSln)!, "MultiFile", "Services");
        Directory.CreateDirectory(sourceFolder);
        
        await File.WriteAllTextAsync(Path.Combine(sourceFolder, "ServiceA.cs"), 
            "namespace MultiFile.Services; public class ServiceA { }");
        await File.WriteAllTextAsync(Path.Combine(sourceFolder, "ServiceB.cs"), 
            "namespace MultiFile.Services; public class ServiceB { }");
        await File.WriteAllTextAsync(Path.Combine(sourceFolder, "IService.cs"), 
            "namespace MultiFile.Services; public interface IService { }");

        var targetFolder = Path.Combine(Path.GetDirectoryName(targetSln)!, "MultiFileCopy", "Services");

        var options = new CrossSolutionMigrationOptions
        {
            SourceSolutionPath = sourceSln,
            TargetSolutionPath = targetSln,
            SourcePath = sourceFolder,
            TargetPath = targetFolder,
            PreserveOriginal = true,
            DryRun = false
        };

        // Act
        var result = await _service.MigrateFolderAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.MigratedFilesCount.Should().Be(3);
        result.UpdatedNamespaces.Should().HaveCount(3);
        
        Directory.GetFiles(targetFolder, "*.cs").Should().HaveCount(3);
    }

    private async Task<string> CreateTestSolutionAsync(string projectName)
    {
        var slnDir = Path.Combine(_testDir, projectName);
        Directory.CreateDirectory(slnDir);
        
        var slnPath = Path.Combine(slnDir, $"{projectName}.sln");
        var slnContent = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""{projectName}\\{projectName}.csproj"", ""{{GUID}}""
EndProject
";
        await File.WriteAllTextAsync(slnPath, slnContent);
        
        var projDir = Path.Combine(slnDir, projectName);
        Directory.CreateDirectory(projDir);
        
        var csprojPath = Path.Combine(projDir, $"{projectName}.csproj");
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(csprojPath, csprojContent);
        
        return slnPath;
    }
}
