using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Analyzers;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Analyzers;

/// <summary>
/// Tests for SolutionAnalyzer - parses .sln files.
/// </summary>
public class SolutionAnalyzerTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<IProjectAnalyzer> _projectAnalyzerMock;
    private readonly Mock<ILogger<SolutionAnalyzer>> _loggerMock;

    public SolutionAnalyzerTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _projectAnalyzerMock = new Mock<IProjectAnalyzer>();
        _loggerMock = new Mock<ILogger<SolutionAnalyzer>>();
    }

    private SolutionAnalyzer CreateAnalyzer()
    {
        return new SolutionAnalyzer(_fileSystemMock.Object, _projectAnalyzerMock.Object, _loggerMock.Object);
    }

    #region AnalyzeSolutionAsync Tests

    [Fact]
    public async Task AnalyzeSolutionAsync_WithValidSolution_ReturnsSolutionInfo()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var solutionContent = CreateSampleSolutionContent("Project1", @"Project1\Project1.csproj");

        SetupFileSystem(solutionPath, solutionContent);
        SetupProjectAnalyzer(@"C:\test\Project1\Project1.csproj", "Project1");

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
        result.Path.Should().Be(solutionPath);
        result.Projects.Should().HaveCount(1);
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\NonExistent.sln";

        _fileSystemMock.Setup(x => x.ExistsAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            analyzer.AnalyzeSolutionAsync(solutionPath));
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_ParsesMultipleProjects()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var solutionContent = CreateMultiProjectSolutionContent();

        SetupFileSystem(solutionPath, solutionContent);
        SetupProjectAnalyzer(@"C:\test\Project1\Project1.csproj", "Project1");
        SetupProjectAnalyzer(@"C:\test\Project2\Project2.csproj", "Project2");

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Projects.Should().HaveCount(2);
        result.Projects.Should().Contain(p => p.Name == "Project1");
        result.Projects.Should().Contain(p => p.Name == "Project2");
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_SkipsSolutionFolders()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var solutionContent = CreateSolutionWithFolders();

        SetupFileSystem(solutionPath, solutionContent);
        SetupProjectAnalyzer(@"C:\test\Project1\Project1.csproj", "Project1");

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Projects.Should().HaveCount(1);
        // Note: Solution folders are parsed but the count may vary based on regex implementation
        // The key assertion is that solution folders don't appear as projects
        result.Projects.Should().NotContain(p => p.Name == "src");
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_SkipsNonCsprojFiles()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var solutionContent = CreateSolutionWithNonCsprojFiles();

        SetupFileSystem(solutionPath, solutionContent);
        SetupProjectAnalyzer(@"C:\test\Project1\Project1.csproj", "Project1");

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Projects.Should().HaveCount(1);
        result.Projects.Single().Name.Should().Be("Project1");
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_HandlesProjectFileNotFound()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var solutionContent = CreateSampleSolutionContent("Project1", @"Project1\Project1.csproj");

        _fileSystemMock.Setup(x => x.ExistsAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solutionContent);
        _fileSystemMock.Setup(x => x.ExistsAsync(It.Is<string>(s => s.EndsWith(".csproj")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_HandlesProjectAnalysisException()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var solutionContent = CreateSampleSolutionContent("Project1", @"Project1\Project1.csproj");

        SetupFileSystem(solutionPath, solutionContent);
        _projectAnalyzerMock.Setup(x => x.AnalyzeProjectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Analysis failed"));

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Projects.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeSolutionAsync_ExtractsProjectGuid()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var solutionPath = @"C:\test\Test.sln";
        var projectGuid = "12345678-1234-1234-1234-123456789012";
        var solutionContent = CreateSampleSolutionContent("Project1", @"Project1\Project1.csproj", projectGuid);

        SetupFileSystem(solutionPath, solutionContent);
        SetupProjectAnalyzer(@"C:\test\Project1\Project1.csproj", "Project1");

        // Act
        var result = await analyzer.AnalyzeSolutionAsync(solutionPath);

        // Assert
        result.Projects.Should().HaveCount(1);
        result.Projects.Single().ProjectGuid.Should().Be(Guid.Parse(projectGuid));
    }

    #endregion

    #region FindSolutionsAsync Tests

    [Fact]
    public async Task FindSolutionsAsync_WithValidDirectory_ReturnsSolutions()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var directoryPath = @"C:\test";
        var solutions = new[] { @"C:\test\Solution1.sln", @"C:\test\Solution2.sln" };

        _fileSystemMock.Setup(x => x.ExistsAsync(directoryPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.GetFilesAsync(directoryPath, "*.sln", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solutions);
        _fileSystemMock.Setup(x => x.GetFilesAsync(directoryPath, "*.sln", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(solutions);

        // Act
        var result = await analyzer.FindSolutionsAsync(directoryPath);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindSolutionsAsync_WithNonExistentDirectory_ReturnsEmpty()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var directoryPath = @"C:\nonexistent";

        _fileSystemMock.Setup(x => x.ExistsAsync(directoryPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await analyzer.FindSolutionsAsync(directoryPath);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindSolutionsAsync_LimitsDepth()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var directoryPath = @"C:\test";
        var shallowSolution = @"C:\test\sub\Solution.sln";
        var deepSolution = @"C:\test\a\b\c\d\Deep.sln";

        _fileSystemMock.Setup(x => x.ExistsAsync(directoryPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.GetFilesAsync(directoryPath, "*.sln", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _fileSystemMock.Setup(x => x.GetFilesAsync(directoryPath, "*.sln", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { shallowSolution, deepSolution });

        // Act
        var result = await analyzer.FindSolutionsAsync(directoryPath);

        // Assert
        result.Should().Contain(shallowSolution);
        result.Should().NotContain(deepSolution);
    }

    [Fact]
    public async Task FindSolutionsAsync_DeduplicatesResults()
    {
        // Arrange
        var analyzer = CreateAnalyzer();
        var directoryPath = @"C:\test";
        var solution = @"C:\test\Solution.sln";

        _fileSystemMock.Setup(x => x.ExistsAsync(directoryPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.GetFilesAsync(directoryPath, "*.sln", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { solution });
        _fileSystemMock.Setup(x => x.GetFilesAsync(directoryPath, "*.sln", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { solution }); // Same solution returned by both

        // Act
        var result = await analyzer.FindSolutionsAsync(directoryPath);

        // Assert
        result.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private void SetupFileSystem(string solutionPath, string content)
    {
        _fileSystemMock.Setup(x => x.ExistsAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(solutionPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
        _fileSystemMock.Setup(x => x.ExistsAsync(It.Is<string>(s => s.EndsWith(".csproj")), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void SetupProjectAnalyzer(string projectPath, string projectName)
    {
        var projectInfo = CreateProjectInfo(projectName, projectPath);
        _projectAnalyzerMock.Setup(x => x.AnalyzeProjectAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projectInfo);
    }

    private static ProjectInfo CreateProjectInfo(string name, string path)
    {
        return new ProjectInfo
        {
            Name = name,
            Path = path,
            IsTestProject = false,
            TargetFramework = "net9.0"
        };
    }

    private static string CreateSampleSolutionContent(string projectName, string projectPath, string? projectGuid = null)
    {
        var guid = projectGuid ?? Guid.NewGuid().ToString();
        return $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""{projectPath}"", ""{{{guid}}}""
EndProject
Global
EndGlobal
";
    }

    private static string CreateMultiProjectSolutionContent()
    {
        return @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project1"", ""Project1\Project1.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project2"", ""Project2\Project2.csproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject
Global
EndGlobal
";
    }

    private static string CreateSolutionWithFolders()
    {
        return @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{2150E333-8FDC-42A3-9474-1A3956D46DE8}"") = ""src"", ""src"", ""{33333333-3333-3333-3333-333333333333}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project1"", ""Project1\Project1.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Global
EndGlobal
";
    }

    private static string CreateSolutionWithNonCsprojFiles()
    {
        return @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project1"", ""Project1\Project1.csproj"", ""{11111111-1111-1111-1111-111111111111}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""NativeProject"", ""NativeProject\NativeProject.vcxproj"", ""{22222222-2222-2222-2222-222222222222}""
EndProject
Global
EndGlobal
";
    }

    #endregion
}
