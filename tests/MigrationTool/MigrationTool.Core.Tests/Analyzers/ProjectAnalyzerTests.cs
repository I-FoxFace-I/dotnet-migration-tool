using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Analyzers;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Analyzers;

public class ProjectAnalyzerTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<ICodeAnalyzer> _codeAnalyzerMock;
    private readonly ProjectAnalyzer _analyzer;

    public ProjectAnalyzerTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _codeAnalyzerMock = new Mock<ICodeAnalyzer>();
        _analyzer = new ProjectAnalyzer(_fileSystemMock.Object, _codeAnalyzerMock.Object, NullLogger<ProjectAnalyzer>.Instance);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_SimpleProject_ExtractsBasicInfo()
    {
        // Arrange
        var projectPath = "/test/MyProject/MyProject.csproj";
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <RootNamespace>MyProject</RootNamespace>
                <AssemblyName>MyProject</AssemblyName>
              </PropertyGroup>
            </Project>
            """;

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(projectPath);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MyProject");
        result.Path.Should().Be(projectPath);
        result.TargetFramework.Should().Be("net9.0");
        result.RootNamespace.Should().Be("MyProject");
        result.AssemblyName.Should().Be("MyProject");
        result.IsTestProject.Should().BeFalse();
    }

    [Fact]
    public async Task AnalyzeProjectAsync_TestProject_DetectsTestProject()
    {
        // Arrange
        var projectPath = "/test/MyProject.Tests/MyProject.Tests.csproj";
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
                <IsTestProject>true</IsTestProject>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
                <PackageReference Include="xunit" Version="2.9.2" />
              </ItemGroup>
            </Project>
            """;

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(projectPath);

        // Assert
        result.IsTestProject.Should().BeTrue();
        result.ProjectType.Should().Be(ProjectType.Test);
        result.PackageReferences.Should().Contain(p => p.Name == "xunit");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithProjectReferences_ExtractsReferences()
    {
        // Arrange
        var projectPath = "/test/MyProject/MyProject.csproj";
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Core\Core.csproj" />
                <ProjectReference Include="..\Services\Services.csproj" />
              </ItemGroup>
            </Project>
            """;

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(projectPath);

        // Assert
        result.ProjectReferences.Should().HaveCount(2);
        result.ProjectReferences.Should().Contain(p => p.Name == "Core");
        result.ProjectReferences.Should().Contain(p => p.Name == "Services");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WpfProject_DetectsProjectType()
    {
        // Arrange
        var projectPath = "/test/WpfApp/WpfApp.csproj";
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0-windows</TargetFramework>
                <UseWPF>true</UseWPF>
                <OutputType>WinExe</OutputType>
              </PropertyGroup>
            </Project>
            """;

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(projectPath);

        // Assert
        result.ProjectType.Should().Be(ProjectType.Wpf);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_MauiProject_DetectsProjectType()
    {
        // Arrange
        var projectPath = "/test/MauiApp/MauiApp.csproj";
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net9.0-android;net9.0-ios</TargetFrameworks>
                <UseMaui>true</UseMaui>
              </PropertyGroup>
            </Project>
            """;

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(projectPath);

        // Assert
        result.ProjectType.Should().Be(ProjectType.Maui);
        result.TargetFrameworks.Should().Contain(["net9.0-android", "net9.0-ios"]);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_DetectsTestProjectByNaming()
    {
        // Arrange
        var projectPath = "/test/MyProject.Tests/MyProject.Tests.csproj";
        var content = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fileSystemMock.Setup(x => x.ReadFileAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(projectPath);

        // Assert
        result.IsTestProject.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeProjectAsync_ProjectNotFound_ThrowsException()
    {
        // Arrange
        var projectPath = "/test/NonExistent/NonExistent.csproj";

        _fileSystemMock.Setup(x => x.ExistsAsync(projectPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _analyzer.AnalyzeProjectAsync(projectPath));
    }

    [Fact]
    public async Task EnrichProjectAsync_AddsSourceFiles()
    {
        // Arrange
        var project = new ProjectInfo
        {
            Name = "MyProject",
            Path = "/test/MyProject/MyProject.csproj"
        };

        var sourceFiles = new List<SourceFileInfo>
        {
            new() { Name = "Class1.cs", Path = "/test/MyProject/Class1.cs" },
            new() { Name = "Class2.cs", Path = "/test/MyProject/Class2.cs" }
        };

        _fileSystemMock.Setup(x => x.ExistsAsync(project.Directory, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _codeAnalyzerMock.Setup(x => x.ScanDirectoryAsync(project.Directory, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceFiles);

        // Act
        var result = await _analyzer.EnrichProjectAsync(project);

        // Assert
        result.SourceFiles.Should().HaveCount(2);
        result.FileCount.Should().Be(2);
    }
}
