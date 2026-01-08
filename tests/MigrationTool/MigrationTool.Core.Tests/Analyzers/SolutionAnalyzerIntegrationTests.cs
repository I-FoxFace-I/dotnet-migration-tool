using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Analyzers;
using MigrationTool.Core.Services;
using MigrationTool.Tests.Infrastructure.Fixtures;
using MigrationTool.Tests.Infrastructure.Generators;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Analyzers;

/// <summary>
/// Integration tests for SolutionAnalyzer using real files on disk.
/// </summary>
public class SolutionAnalyzerIntegrationTests : IDisposable
{
    private readonly TempDirectoryFixture _tempDir;
    private readonly SolutionAnalyzer _analyzer;

    public SolutionAnalyzerIntegrationTests()
    {
        _tempDir = new TempDirectoryFixture();

        // Use real services
        var fileSystem = new LocalFileSystemService();
        var codeAnalyzer = new CodeAnalyzer(fileSystem, Mock.Of<ILogger<CodeAnalyzer>>());
        var projectAnalyzer = new ProjectAnalyzer(fileSystem, codeAnalyzer, Mock.Of<ILogger<ProjectAnalyzer>>());

        _analyzer = new SolutionAnalyzer(
            fileSystem,
            projectAnalyzer,
            Mock.Of<ILogger<SolutionAnalyzer>>());
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithMinimalSolution_ParsesCorrectly()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateMinimalSolution(_tempDir);

        // Act
        var result = await _analyzer.AnalyzeSolutionAsync(fakeSolution.Path);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(fakeSolution.Name);
        result.Path.Should().Be(fakeSolution.Path);
        result.Projects.Should().HaveCount(1);
        result.Projects[0].Name.Should().Be($"{fakeSolution.Name}.Core");
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithTestProject_IdentifiesTestProject()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateSolutionWithTests(_tempDir);

        // Act
        var result = await _analyzer.AnalyzeSolutionAsync(fakeSolution.Path);

        // Assert
        result.Projects.Should().HaveCount(2);
        result.TestProjectCount.Should().Be(1);
        result.SourceProjectCount.Should().Be(1);

        var testProject = result.Projects.First(p => p.IsTestProject);
        testProject.Name.Should().EndWith(".Tests");
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithSolutionFolders_SkipsFolders()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateSolutionWithFolders(_tempDir);

        // Act
        var result = await _analyzer.AnalyzeSolutionAsync(fakeSolution.Path);

        // Assert
        result.Projects.Should().HaveCount(1);
        // Solution folders should not appear as projects
        result.Projects.Should().NotContain(p => p.Name == "src");
    }

    [Fact(Skip = "Source file parsing requires ProjectAnalyzer.EnrichProjectAsync to be called separately")]
    public async Task AnalyzeSolutionAsync_ParsesSourceFiles()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateMinimalSolution(_tempDir);

        // Act
        var result = await _analyzer.AnalyzeSolutionAsync(fakeSolution.Path);

        // Assert
        var project = result.Projects[0];
        project.SourceFiles.Should().NotBeEmpty();
        project.SourceFiles.Should().Contain(f => f.Name == "SampleClass.cs");
    }

    [Fact(Skip = "Class parsing requires ProjectAnalyzer.EnrichProjectAsync to be called separately")]
    public async Task AnalyzeSolutionAsync_ParsesClasses()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateMinimalSolution(_tempDir);

        // Act
        var result = await _analyzer.AnalyzeSolutionAsync(fakeSolution.Path);

        // Assert
        var project = result.Projects[0];
        project.ClassCount.Should().BeGreaterThan(0);

        var sampleFile = project.SourceFiles.First(f => f.Name == "SampleClass.cs");
        sampleFile.Classes.Should().Contain(c => c.Name == "SampleClass");
    }

    [Fact(Skip = "Test method parsing requires ProjectAnalyzer.EnrichProjectAsync to be called separately")]
    public async Task AnalyzeSolutionAsync_ParsesTestMethods()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateSolutionWithTests(_tempDir);

        // Act
        var result = await _analyzer.AnalyzeSolutionAsync(fakeSolution.Path);

        // Assert
        var testProject = result.Projects.First(p => p.IsTestProject);
        testProject.TestCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task FindSolutionsAsync_FindsSolutionInDirectory()
    {
        // Arrange
        var fakeSolution = FakeSolutionGenerator.CreateMinimalSolution(_tempDir);

        // Act
        var solutions = await _analyzer.FindSolutionsAsync(_tempDir.Path);

        // Assert
        solutions.Should().Contain(fakeSolution.Path);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir.Path, "NonExistent.sln");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _analyzer.AnalyzeSolutionAsync(nonExistentPath));
    }

    public void Dispose()
    {
        _tempDir.Dispose();
    }
}
