using System.Diagnostics;
using FluentAssertions;
using MigrationTool.Tests.Infrastructure.Fixtures;
using MigrationTool.Tests.Infrastructure.Generators;
using Xunit;
using Xunit.Abstractions;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests that verify generated fake solutions can be built.
/// </summary>
public class FakeProjectBuildTests : IDisposable
{
    private readonly TempDirectoryFixture _tempDir;
    private readonly ITestOutputHelper _output;
    
    public FakeProjectBuildTests(ITestOutputHelper output)
    {
        _tempDir = new TempDirectoryFixture();
        _output = output;
    }
    
    public void Dispose()
    {
        _tempDir.Dispose();
    }
    
    [Fact]
    public async Task CreateEfCoreSolution_CanBeBuilt()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, "BuildTestApp");
        
        _output.WriteLine($"Solution path: {solution.Path}");
        _output.WriteLine($"Solution directory: {solution.Directory}");
        _output.WriteLine($"Projects count: {solution.Projects.Count}");
        
        foreach (var project in solution.Projects)
        {
            _output.WriteLine($"  - {project.Name}: {project.Path}");
            _output.WriteLine($"    Files: {project.SourceFiles.Count}, Folders: {string.Join(", ", project.Folders)}");
        }
        
        // Act - try to build the solution
        var (exitCode, output, error) = await RunDotnetAsync("build", solution.Path);
        
        _output.WriteLine("=== BUILD OUTPUT ===");
        _output.WriteLine(output);
        if (!string.IsNullOrEmpty(error))
        {
            _output.WriteLine("=== BUILD ERRORS ===");
            _output.WriteLine(error);
        }
        
        // Assert
        exitCode.Should().Be(0, $"Build should succeed. Errors:\n{error}\n\nOutput:\n{output}");
    }
    
    [Fact]
    public async Task CreateMinimalSolution_CanBeBuilt()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateMinimalSolution(_tempDir, "MinimalBuildTest");
        
        // Act
        var (exitCode, output, error) = await RunDotnetAsync("build", solution.Path);
        
        _output.WriteLine(output);
        
        // Assert
        exitCode.Should().Be(0, $"Build should succeed. Errors:\n{error}");
    }
    
    [Fact]
    public async Task CreateSolutionWithTests_CanBeBuilt()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateSolutionWithTests(_tempDir, "TestsBuildTest");
        
        // Act
        var (exitCode, output, error) = await RunDotnetAsync("build", solution.Path);
        
        _output.WriteLine(output);
        
        // Assert
        exitCode.Should().Be(0, $"Build should succeed. Errors:\n{error}");
    }
    
    private static async Task<(int ExitCode, string Output, string Error)> RunDotnetAsync(string command, string solutionPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{command} \"{solutionPath}\" --no-restore",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(solutionPath)
        };
        
        // First restore
        var restorePsi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"restore \"{solutionPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(solutionPath)
        };
        
        using var restoreProcess = Process.Start(restorePsi)!;
        await restoreProcess.WaitForExitAsync();
        
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        return (process.ExitCode, output.Trim(), error.Trim());
    }
}
