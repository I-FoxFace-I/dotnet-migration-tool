using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Services;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Services;

/// <summary>
/// Unit tests for MigrationExecutor - uses mocks for fast isolated testing.
/// For integration tests with real file system, see MigrationExecutorIntegrationTests.
/// </summary>
public class MigrationExecutorTests
{
    #region Test Constants

    private static class Paths
    {
        public const string WorkspaceRoot = @"C:\workspace";

        public static class Source
        {
            public const string File = @"C:\source\file.cs";
            public const string File1 = @"C:\source\file1.cs";
            public const string File2 = @"C:\source\file2.cs";
            public const string Folder = @"C:\source\folder";
            public const string RelativeFile = @"src\file.cs";
        }

        public static class Target
        {
            public const string File = @"C:\target\file.cs";
            public const string File1 = @"C:\target\file1.cs";
            public const string File2 = @"C:\target\file2.cs";
            public const string Folder = @"C:\target\folder";
            public const string RelativeFile = @"target\file.cs";
        }

        public static class Workspace
        {
            public const string SourceFile = @"C:\workspace\src\file.cs";
            public const string TargetFile = @"C:\workspace\target\file.cs";
        }

        public static class Project
        {
            public const string Main = @"C:\project\Project.csproj";
            public const string Reference = @"C:\reference\Reference.csproj";
            public const string MyProject = @"C:\workspace\src\MyProject\MyProject.csproj";
            public const string Other = @"C:\workspace\src\Other\Other.csproj";
            public const string Test = @"C:\workspace\test\Test\Test.csproj";
        }
    }

    private static class Namespaces
    {
        public const string Old = "OldNamespace";
        public const string New = "NewNamespace";
        public const string OldDotted = "Old.Namespace";
        public const string NewDotted = "New.Namespace";
    }

    private static class TestContent
    {
        public const string SimpleClass = $"namespace {Namespaces.Old};\n\nclass Test {{ }}";
        public const string ClassWithUsing = $"using {Namespaces.OldDotted};\n\nnamespace Other;\n\nclass Test {{ }}";

        public static readonly string EmptyProject = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net9.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        public static readonly string ProjectWithReference = """
            <ProjectReference Include="..\MyProject\MyProject.csproj" />
            """;
    }

    private static class PlanNames
    {
        public const string Empty = "Empty Plan";
        public const string Test = "Test Plan";
    }

    #endregion

    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<ILogger<MigrationExecutor>> _loggerMock;

    public MigrationExecutorTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _loggerMock = new Mock<ILogger<MigrationExecutor>>();
    }

    private MigrationExecutor CreateExecutor(bool dryRun = false, string? workspaceRoot = null)
    {
        return new MigrationExecutor(_fileSystemMock.Object, _loggerMock.Object)
        {
            DryRun = dryRun,
            WorkspaceRoot = workspaceRoot
        };
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WithEmptyPlan_ReturnsSuccessWithNoSteps()
    {
        // Arrange
        var executor = CreateExecutor();
        var plan = new MigrationPlan
        {
            Name = PlanNames.Empty,
            Steps = []
        };

        // Act
        var result = await executor.ExecuteAsync(plan);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.StepResults.Should().BeEmpty();
        result.Plan.Status.Should().Be(PlanStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleMoveFileStep_ExecutesSuccessfully()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);
        var plan = CreatePlanWithSteps(step);

        SetupFileExists(Paths.Source.File, true);
        SetupFileExists(Paths.Target.File, false);
        SetupMoveSucceeds(Paths.Source.File, Paths.Target.File);

        // Act
        var result = await executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        result.StepResults.Should().HaveCount(1);
        result.StepResults[0].Success.Should().BeTrue();
        result.Plan.Status.Should().Be(PlanStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedStep_StopsExecutionAndMarksRemainingAsSkipped()
    {
        // Arrange
        var executor = CreateExecutor();
        var step1 = CreateMoveFileStep(Paths.Source.File1, Paths.Target.File1);
        var step2 = CreateMoveFileStep(Paths.Source.File2, Paths.Target.File2);
        var plan = CreatePlanWithSteps(step1, step2);

        // First step fails (source doesn't exist)
        SetupFileExists(Paths.Source.File1, false);

        // Act
        var result = await executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeFalse();
        result.StepResults.Should().HaveCount(1);
        result.StepResults[0].Success.Should().BeFalse();
        result.Plan.Steps[0].Status.Should().Be(StepStatus.Failed);
        result.Plan.Steps[1].Status.Should().Be(StepStatus.Skipped);
        result.Plan.Status.Should().Be(PlanStatus.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgress()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);
        var plan = CreatePlanWithSteps(step);

        SetupFileExists(Paths.Source.File, true);
        SetupFileExists(Paths.Target.File, false);
        SetupMoveSucceeds(Paths.Source.File, Paths.Target.File);

        var progressReports = new List<MigrationProgress>();
        var progress = new Progress<MigrationProgress>(p => progressReports.Add(p));

        // Act
        await executor.ExecuteAsync(plan, progress);

        // Assert - wait a bit for progress to be reported
        await Task.Delay(50);
        progressReports.Should().NotBeEmpty();
        progressReports.Last().TotalSteps.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsCancellation()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);
        var plan = CreatePlanWithSteps(step);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            executor.ExecuteAsync(plan, null, cts.Token));
    }

    #endregion

    #region ExecuteStepAsync - MoveFile Tests

    [Fact]
    public async Task ExecuteStepAsync_MoveFile_WithValidPaths_Succeeds()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);

        SetupFileExists(Paths.Source.File, true);
        SetupFileExists(Paths.Target.File, false);
        SetupMoveSucceeds(Paths.Source.File, Paths.Target.File);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        result.Step.Status.Should().Be(StepStatus.Completed);
        _fileSystemMock.Verify(x => x.MoveAsync(Paths.Source.File, Paths.Target.File, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteStepAsync_MoveFile_WhenSourceNotExists_Fails()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);

        SetupFileExists(Paths.Source.File, false);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("does not exist");
        result.Step.Status.Should().Be(StepStatus.Failed);
    }

    [Fact]
    public async Task ExecuteStepAsync_MoveFile_WhenTargetExists_Fails()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);

        SetupFileExists(Paths.Source.File, true);
        SetupFileExists(Paths.Target.File, true);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public async Task ExecuteStepAsync_MoveFile_InDryRunMode_DoesNotMove()
    {
        // Arrange
        var executor = CreateExecutor(dryRun: true);
        var step = CreateMoveFileStep(Paths.Source.File, Paths.Target.File);

        SetupFileExists(Paths.Source.File, true);
        SetupFileExists(Paths.Target.File, false);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.MoveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region ExecuteStepAsync - MoveFolder Tests

    [Fact]
    public async Task ExecuteStepAsync_MoveFolder_WithValidPaths_Succeeds()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFolderStep(Paths.Source.Folder, Paths.Target.Folder);

        SetupFileExists(Paths.Source.Folder, true);
        SetupIsDirectory(Paths.Source.Folder, true);
        SetupFileExists(Paths.Target.Folder, false);
        SetupMoveSucceeds(Paths.Source.Folder, Paths.Target.Folder);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.MoveAsync(Paths.Source.Folder, Paths.Target.Folder, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteStepAsync_MoveFolder_WhenSourceIsNotDirectory_Fails()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateMoveFolderStep(Paths.Source.File, Paths.Target.Folder);

        SetupFileExists(Paths.Source.File, true);
        SetupIsDirectory(Paths.Source.File, false);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not a directory");
    }

    #endregion

    #region ExecuteStepAsync - CopyFile Tests

    [Fact]
    public async Task ExecuteStepAsync_CopyFile_WithValidPaths_Succeeds()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateCopyFileStep(Paths.Source.File, Paths.Target.File);

        SetupFileExists(Paths.Source.File, true);
        SetupFileExists(Paths.Target.File, false);
        SetupCopySucceeds(Paths.Source.File, Paths.Target.File);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.CopyAsync(Paths.Source.File, Paths.Target.File, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ExecuteStepAsync - RenameNamespace Tests

    [Fact]
    public async Task ExecuteStepAsync_RenameNamespace_WithValidMetadata_Succeeds()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateRenameNamespaceStep(
            Paths.Source.File,
            Namespaces.New,
            new Dictionary<string, string> { ["OldNamespace"] = Namespaces.Old });

        SetupFileExists(Paths.Source.File, true);
        SetupReadFile(Paths.Source.File, TestContent.SimpleClass);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.WriteFileAsync(
            Paths.Source.File,
            It.Is<string>(s => s.Contains($"namespace {Namespaces.New}")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteStepAsync_RenameNamespace_WithMissingOldNamespace_Fails()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateRenameNamespaceStep(Paths.Source.File, Namespaces.New, new Dictionary<string, string>());

        SetupFileExists(Paths.Source.File, true);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("OldNamespace");
    }

    [Fact]
    public async Task ExecuteStepAsync_RenameNamespace_ReplacesUsingStatements()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = CreateRenameNamespaceStep(
            Paths.Source.File,
            Namespaces.NewDotted,
            new Dictionary<string, string> { ["OldNamespace"] = Namespaces.OldDotted });

        SetupFileExists(Paths.Source.File, true);
        SetupReadFile(Paths.Source.File, TestContent.ClassWithUsing);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.WriteFileAsync(
            Paths.Source.File,
            It.Is<string>(s => s.Contains($"using {Namespaces.NewDotted}")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ExecuteStepAsync - AddProjectReference Tests

    [Fact]
    public async Task ExecuteStepAsync_AddProjectReference_AddsReferenceToProject()
    {
        // Arrange
        var executor = CreateExecutor();
        var step = new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.AddProjectReference,
            Source = Paths.Project.Main,
            Target = Paths.Project.Reference
        };

        SetupFileExists(Paths.Project.Main, true);
        SetupReadFile(Paths.Project.Main, TestContent.EmptyProject);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.WriteFileAsync(
            Paths.Project.Main,
            It.Is<string>(s => s.Contains("ProjectReference")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_CreatesReversePlan()
    {
        // Arrange
        var executor = CreateExecutor();
        var completedStep = CreateMoveFileStep(Paths.Source.File, Paths.Target.File) with
        {
            Status = StepStatus.Completed
        };
        var plan = CreatePlanWithSteps(completedStep) with
        {
            Status = PlanStatus.Completed
        };

        // Setup for reverse move
        SetupFileExists(Paths.Target.File, true);
        SetupFileExists(Paths.Source.File, false);
        SetupMoveSucceeds(Paths.Target.File, Paths.Source.File);

        // Act
        var result = await executor.RollbackAsync(plan);

        // Assert
        result.Plan.Name.Should().StartWith("Rollback:");
        result.Plan.Steps.Should().HaveCount(1);
        result.Plan.Steps[0].Source.Should().Be(Paths.Target.File);
        result.Plan.Steps[0].Target.Should().Be(Paths.Source.File);
    }

    [Fact]
    public async Task RollbackAsync_SkipsNonReversibleSteps()
    {
        // Arrange
        var executor = CreateExecutor();
        var copyStep = CreateCopyFileStep(Paths.Source.File, Paths.Target.File) with
        {
            Status = StepStatus.Completed
        };
        var plan = CreatePlanWithSteps(copyStep) with
        {
            Status = PlanStatus.Completed
        };

        // Act
        var result = await executor.RollbackAsync(plan);

        // Assert
        result.Plan.Steps.Should().BeEmpty();
    }

    #endregion

    #region FindAffectedProjectsAsync Tests

    [Fact]
    public async Task FindAffectedProjectsAsync_FindsProjectsWithReferences()
    {
        // Arrange
        var executor = CreateExecutor(workspaceRoot: Paths.WorkspaceRoot);

        _fileSystemMock.Setup(x => x.GetFilesAsync(Paths.WorkspaceRoot, "*.csproj", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { Paths.Project.Other, Paths.Project.Test });

        _fileSystemMock.Setup(x => x.ReadFileAsync(Paths.Project.Other, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestContent.ProjectWithReference);

        _fileSystemMock.Setup(x => x.ReadFileAsync(Paths.Project.Test, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestContent.ProjectWithReference);

        // Act
        var result = await executor.FindAffectedProjectsAsync(Paths.Project.MyProject);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAffectedProjectsAsync_WithoutWorkspaceRoot_ReturnsEmpty()
    {
        // Arrange
        var executor = CreateExecutor(workspaceRoot: null);

        // Act
        var result = await executor.FindAffectedProjectsAsync(Paths.Project.Main);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region WorkspaceRoot Tests

    [Fact]
    public async Task ExecuteStepAsync_WithRelativePath_ResolvesFromWorkspaceRoot()
    {
        // Arrange
        var executor = CreateExecutor(workspaceRoot: Paths.WorkspaceRoot);
        var step = CreateMoveFileStep(Paths.Source.RelativeFile, Paths.Target.RelativeFile);

        SetupFileExists(Paths.Workspace.SourceFile, true);
        SetupFileExists(Paths.Workspace.TargetFile, false);
        SetupMoveSucceeds(Paths.Workspace.SourceFile, Paths.Workspace.TargetFile);

        // Act
        var result = await executor.ExecuteStepAsync(step);

        // Assert
        result.Success.Should().BeTrue();
        _fileSystemMock.Verify(x => x.MoveAsync(
            Paths.Workspace.SourceFile,
            Paths.Workspace.TargetFile,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static MigrationStep CreateMoveFileStep(string source, string target)
    {
        return new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.MoveFile,
            Source = source,
            Target = target
        };
    }

    private static MigrationStep CreateMoveFolderStep(string source, string target)
    {
        return new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.MoveFolder,
            Source = source,
            Target = target
        };
    }

    private static MigrationStep CreateCopyFileStep(string source, string target)
    {
        return new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.CopyFile,
            Source = source,
            Target = target
        };
    }

    private static MigrationStep CreateRenameNamespaceStep(string filePath, string newNamespace, Dictionary<string, string> metadata)
    {
        return new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.RenameNamespace,
            Source = filePath,
            Target = newNamespace,
            Metadata = metadata
        };
    }

    private static MigrationPlan CreatePlanWithSteps(params MigrationStep[] steps)
    {
        var indexedSteps = steps.Select((s, i) => s with { Index = i + 1 }).ToList();
        return new MigrationPlan
        {
            Name = PlanNames.Test,
            Steps = indexedSteps,
            Status = PlanStatus.Ready
        };
    }

    private void SetupFileExists(string path, bool exists)
    {
        _fileSystemMock.Setup(x => x.ExistsAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
    }

    private void SetupIsDirectory(string path, bool isDirectory)
    {
        _fileSystemMock.Setup(x => x.IsDirectoryAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(isDirectory);
    }

    private void SetupMoveSucceeds(string source, string target)
    {
        _fileSystemMock.Setup(x => x.MoveAsync(source, target, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupCopySucceeds(string source, string target)
    {
        _fileSystemMock.Setup(x => x.CopyAsync(source, target, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupReadFile(string path, string content)
    {
        _fileSystemMock.Setup(x => x.ReadFileAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
    }

    #endregion
}
