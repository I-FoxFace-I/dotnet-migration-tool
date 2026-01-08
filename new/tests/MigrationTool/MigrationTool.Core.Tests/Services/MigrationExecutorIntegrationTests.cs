using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Services;
using MigrationTool.Tests.Infrastructure.Fixtures;
using MigrationTool.Tests.Infrastructure.Generators;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Services;

/// <summary>
/// Integration tests for MigrationExecutor using real file system operations.
/// Each test creates a fake .NET solution using FakeSolutionGenerator.
/// TempDirectoryFixture creates a unique directory (with GUID) for each test and cleans up on dispose.
/// </summary>
public class MigrationExecutorIntegrationTests : IDisposable
{
    #region Test Constants

    private static class SolutionNames
    {
        public const string Default = "TestApp";
    }

    private static class ProjectSuffixes
    {
        public const string Domain = ".Domain";
        public const string Infrastructure = ".Infrastructure";
        public const string Tests = ".Tests";
    }

    private static class FolderNames
    {
        public const string Moved = "Moved";
        public const string Entities = "Entities";
        public const string Models = "Models";
        public const string Repositories = "Repositories";
    }

    private static class FileNames
    {
        public const string Customer = "Customer.cs";
        public const string Product = "Product.cs";
        public const string Order = "Order.cs";
        public const string NonExistent = "NonExistent.cs";
        public const string CustomerRepository = "CustomerRepository.cs";
        public const string DomainCsproj = "TestApp.Domain.csproj";
    }

    private static class FilePatterns
    {
        public const string CSharpFiles = "*.cs";
    }

    private static class Namespaces
    {
        public static string GetOld(string solutionName) => $"{solutionName}.Domain.Entities";
        public static string GetNew(string solutionName) => $"{solutionName}.Core.Models";
    }

    private static class PlanNames
    {
        public const string MoveFile = "Move File Plan";
        public const string MoveFolder = "Move Folder Plan";
        public const string RenameNamespace = "Rename Namespace Plan";
        public const string MultiStep = "Multi Step Plan";
        public const string Rollback = "Rollback Plan";
    }

    private static class MetadataKeys
    {
        public const string OldNamespace = "OldNamespace";
    }

    private static class ClassNames
    {
        public const string Customer = "Customer";
    }

    #endregion

    private readonly TempDirectoryFixture _tempDir;
    private readonly LocalFileSystemService _fileSystem;
    private readonly MigrationExecutor _executor;

    public MigrationExecutorIntegrationTests()
    {
        _tempDir = new TempDirectoryFixture();
        _fileSystem = new LocalFileSystemService();
        _executor = new MigrationExecutor(_fileSystem, Mock.Of<ILogger<MigrationExecutor>>())
        {
            WorkspaceRoot = _tempDir.Path
        };
    }

    public void Dispose()
    {
        _tempDir.Dispose();
    }

    #region Path Helper Methods

    private static string GetProjectDirectory(FakeProject project) =>
        Path.GetDirectoryName(project.Path)!;

    private static string GetSubfolder(FakeProject project, string folderName) =>
        Path.Combine(GetProjectDirectory(project), folderName);

    private static string GetFileInFolder(FakeProject project, string folderName, string fileName) =>
        Path.Combine(GetProjectDirectory(project), folderName, fileName);

    private string GetMovedProjectPath(string solutionName, string projectSuffix) =>
        Path.Combine(_tempDir.Path, FolderNames.Moved, $"{solutionName}{projectSuffix}");

    #endregion

    #region MoveFile Integration Tests

    [Fact]
    public async Task ExecuteAsync_MoveFile_MovesEntityFileToNewLocation()
    {
        // Arrange - Create fake solution with real .NET project structure
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var plan = CreatePlan(PlanNames.MoveFile,
            CreateMoveFileStep(sourceFile, targetFile));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue($"Expected success but got: {result.StepResults.FirstOrDefault()?.ErrorMessage}");
        File.Exists(sourceFile).Should().BeFalse("source file should be moved");
        File.Exists(targetFile).Should().BeTrue("target file should exist");

        var content = await File.ReadAllTextAsync(targetFile);
        content.Should().Contain($"class {ClassNames.Customer}");
    }

    [Fact]
    public async Task ExecuteAsync_MoveFile_DryRunDoesNotMoveFile()
    {
        // Arrange
        _executor.DryRun = true;
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var plan = CreatePlan(PlanNames.MoveFile,
            CreateMoveFileStep(sourceFile, targetFile));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(sourceFile).Should().BeTrue("source file should still exist in dry run");
        File.Exists(targetFile).Should().BeFalse("target file should not be created in dry run");
    }

    [Fact]
    public async Task ExecuteAsync_MoveFile_PreservesFileContent()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Product));
        var originalContent = await File.ReadAllTextAsync(sourceFile);
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Product);

        var plan = CreatePlan(PlanNames.MoveFile,
            CreateMoveFileStep(sourceFile, targetFile));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        var movedContent = await File.ReadAllTextAsync(targetFile);
        movedContent.Should().Be(originalContent);
    }

    #endregion

    #region MoveFolder Integration Tests

    [Fact]
    public async Task ExecuteAsync_MoveFolder_MovesEntitiesFolderWithAllFiles()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFolder = GetSubfolder(domainProject, FolderNames.Entities);
        var targetFolder = GetSubfolder(domainProject, FolderNames.Models);

        var originalFiles = Directory.GetFiles(sourceFolder, FilePatterns.CSharpFiles);

        var plan = CreatePlan(PlanNames.MoveFolder,
            CreateMoveFolderStep(sourceFolder, targetFolder));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue($"Expected success but got: {result.StepResults.FirstOrDefault()?.ErrorMessage}");
        Directory.Exists(sourceFolder).Should().BeFalse("source folder should be moved");
        Directory.Exists(targetFolder).Should().BeTrue("target folder should exist");

        var movedFiles = Directory.GetFiles(targetFolder, FilePatterns.CSharpFiles);
        movedFiles.Should().HaveCount(originalFiles.Length);
    }

    [Fact]
    public async Task ExecuteAsync_MoveFolder_MovesProjectFolderToNewLocation()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFolder = GetProjectDirectory(domainProject);
        var targetFolder = GetMovedProjectPath(SolutionNames.Default, ProjectSuffixes.Domain);

        var plan = CreatePlan(PlanNames.MoveFolder,
            CreateMoveFolderStep(sourceFolder, targetFolder));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue($"Expected success but got: {result.StepResults.FirstOrDefault()?.ErrorMessage}");
        Directory.Exists(targetFolder).Should().BeTrue();
        File.Exists(Path.Combine(targetFolder, FileNames.DomainCsproj)).Should().BeTrue();
        File.Exists(Path.Combine(targetFolder, FolderNames.Entities, FileNames.Customer)).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_MoveFolder_DryRunDoesNotMoveFolder()
    {
        // Arrange
        _executor.DryRun = true;
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFolder = GetSubfolder(domainProject, FolderNames.Entities);
        var targetFolder = GetSubfolder(domainProject, FolderNames.Models);

        var plan = CreatePlan(PlanNames.MoveFolder,
            CreateMoveFolderStep(sourceFolder, targetFolder));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        Directory.Exists(sourceFolder).Should().BeTrue("source folder should still exist in dry run");
        Directory.Exists(targetFolder).Should().BeFalse("target folder should not be created in dry run");
    }

    #endregion

    #region RenameNamespace Integration Tests

    [Fact]
    public async Task ExecuteAsync_RenameNamespace_UpdatesNamespaceInEntityFile()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entityFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var oldNamespace = Namespaces.GetOld(SolutionNames.Default);
        var newNamespace = Namespaces.GetNew(SolutionNames.Default);

        var plan = CreatePlan(PlanNames.RenameNamespace,
            CreateRenameNamespaceStep(entityFile, newNamespace, oldNamespace));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue($"Expected success but got: {result.StepResults.FirstOrDefault()?.ErrorMessage}");
        var updatedContent = await File.ReadAllTextAsync(entityFile);
        updatedContent.Should().Contain($"namespace {newNamespace}");
        updatedContent.Should().NotContain($"namespace {oldNamespace}");
    }

    [Fact]
    public async Task ExecuteAsync_RenameNamespace_UpdatesUsingStatementsInRepository()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var infraProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Infrastructure));

        var repositoryFile = infraProject.SourceFiles.First(f => f.EndsWith(FileNames.CustomerRepository));
        var oldNamespace = Namespaces.GetOld(SolutionNames.Default);
        var newNamespace = Namespaces.GetNew(SolutionNames.Default);

        var plan = CreatePlan(PlanNames.RenameNamespace,
            CreateRenameNamespaceStep(repositoryFile, newNamespace, oldNamespace));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        var updatedContent = await File.ReadAllTextAsync(repositoryFile);
        updatedContent.Should().Contain($"using {newNamespace}");
        updatedContent.Should().NotContain($"using {oldNamespace}");
    }

    [Fact]
    public async Task ExecuteAsync_RenameNamespace_DryRunDoesNotModifyFile()
    {
        // Arrange
        _executor.DryRun = true;
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entityFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var originalContent = await File.ReadAllTextAsync(entityFile);
        var oldNamespace = Namespaces.GetOld(SolutionNames.Default);
        var newNamespace = Namespaces.GetNew(SolutionNames.Default);

        var plan = CreatePlan(PlanNames.RenameNamespace,
            CreateRenameNamespaceStep(entityFile, newNamespace, oldNamespace));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        var unchangedContent = await File.ReadAllTextAsync(entityFile);
        unchangedContent.Should().Be(originalContent);
    }

    #endregion

    #region Multi-Step Integration Tests

    [Fact]
    public async Task ExecuteAsync_MultipleSteps_MovesMultipleEntityFiles()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var productFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Product));

        var plan = CreatePlan(PlanNames.MultiStep,
            CreateMoveFileStep(customerFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer)),
            CreateMoveFileStep(productFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Product)));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        result.StepResults.Should().HaveCount(2);
        result.StepResults.Should().OnlyContain(r => r.Success);
        File.Exists(GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer)).Should().BeTrue();
        File.Exists(GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Product)).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithFailingStep_StopsAndSkipsRemainingSteps()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var nonExistentFile = Path.Combine(GetProjectDirectory(domainProject), FileNames.NonExistent);
        var productFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Product));

        var plan = CreatePlan(PlanNames.MultiStep,
            CreateMoveFileStep(customerFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer)),
            CreateMoveFileStep(nonExistentFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.NonExistent)), // This will fail
            CreateMoveFileStep(productFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Product)));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeFalse();
        result.StepResults.Should().HaveCount(2); // First succeeded, second failed
        result.StepResults[0].Success.Should().BeTrue();
        result.StepResults[1].Success.Should().BeFalse();
        result.Plan.Steps[2].Status.Should().Be(StepStatus.Skipped);
    }

    [Fact]
    public async Task ExecuteAsync_MoveFileThenRenameNamespace_ExecutesBothOperations()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var movedFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var oldNamespace = Namespaces.GetOld(SolutionNames.Default);
        var newNamespace = Namespaces.GetNew(SolutionNames.Default);

        var plan = CreatePlan(PlanNames.MultiStep,
            CreateMoveFileStep(customerFile, movedFile),
            CreateRenameNamespaceStep(movedFile, newNamespace, oldNamespace));

        // Act
        var result = await _executor.ExecuteAsync(plan);

        // Assert
        result.Success.Should().BeTrue();
        File.Exists(movedFile).Should().BeTrue();
        var content = await File.ReadAllTextAsync(movedFile);
        content.Should().Contain($"namespace {newNamespace}");
    }

    #endregion

    #region FindAffectedProjects Integration Tests

    [Fact]
    public async Task FindAffectedProjectsAsync_FindsProjectsReferencingDomainProject()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        // Act
        var affectedProjects = await _executor.FindAffectedProjectsAsync(domainProject.Path);

        // Assert
        affectedProjects.Should().NotBeEmpty();
        affectedProjects.Should().Contain(p => p.Contains(ProjectSuffixes.Infrastructure));
        affectedProjects.Should().Contain(p => p.Contains(ProjectSuffixes.Tests));
    }

    [Fact]
    public async Task FindAffectedProjectsAsync_InfrastructureProjectHasNoReferences()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var infraProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Infrastructure));

        // Act
        var affectedProjects = await _executor.FindAffectedProjectsAsync(infraProject.Path);

        // Assert
        // Infrastructure is referenced by Tests, so it should find Tests
        affectedProjects.Should().Contain(p => p.Contains(ProjectSuffixes.Tests));
    }

    #endregion

    #region Rollback Integration Tests

    [Fact]
    public async Task RollbackAsync_AfterMoveFile_RestoresOriginalLocation()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var originalContent = await File.ReadAllTextAsync(sourceFile);
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var plan = CreatePlan(PlanNames.Rollback,
            CreateMoveFileStep(sourceFile, targetFile));

        // Execute the plan first
        var executeResult = await _executor.ExecuteAsync(plan);
        executeResult.Success.Should().BeTrue();
        File.Exists(targetFile).Should().BeTrue();
        File.Exists(sourceFile).Should().BeFalse();

        // Act - Rollback
        var rollbackResult = await _executor.RollbackAsync(executeResult.Plan);

        // Assert
        rollbackResult.Success.Should().BeTrue();
        File.Exists(sourceFile).Should().BeTrue("original file should be restored");
        File.Exists(targetFile).Should().BeFalse("target file should be moved back");
        var restoredContent = await File.ReadAllTextAsync(sourceFile);
        restoredContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task RollbackAsync_AfterMoveFolder_RestoresOriginalLocation()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFolder = GetSubfolder(domainProject, FolderNames.Entities);
        var targetFolder = GetSubfolder(domainProject, FolderNames.Models);
        var originalFileCount = Directory.GetFiles(sourceFolder, FilePatterns.CSharpFiles).Length;

        var plan = CreatePlan(PlanNames.Rollback,
            CreateMoveFolderStep(sourceFolder, targetFolder));

        // Execute the plan first
        var executeResult = await _executor.ExecuteAsync(plan);
        executeResult.Success.Should().BeTrue();
        Directory.Exists(targetFolder).Should().BeTrue();

        // Act - Rollback
        var rollbackResult = await _executor.RollbackAsync(executeResult.Plan);

        // Assert
        rollbackResult.Success.Should().BeTrue();
        Directory.Exists(sourceFolder).Should().BeTrue("original folder should be restored");
        Directory.Exists(targetFolder).Should().BeFalse("target folder should be moved back");
        Directory.GetFiles(sourceFolder, FilePatterns.CSharpFiles).Should().HaveCount(originalFileCount);
    }

    #endregion

    #region Progress Reporting Integration Tests

    [Fact]
    public async Task ExecuteAsync_ReportsProgressForEachStep()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var productFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Product));

        var plan = CreatePlan(PlanNames.MultiStep,
            CreateMoveFileStep(customerFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer)),
            CreateMoveFileStep(productFile, GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Product)));

        var progressReports = new List<MigrationProgress>();
        var progress = new Progress<MigrationProgress>(p => progressReports.Add(p));

        // Act
        await _executor.ExecuteAsync(plan, progress);

        // Assert - wait a bit for progress to be reported
        await Task.Delay(100);
        progressReports.Should().NotBeEmpty();
        progressReports.Last().TotalSteps.Should().Be(2);
    }

    #endregion

    #region Helper Methods

    private static MigrationPlan CreatePlan(string name, params MigrationStep[] steps)
    {
        var indexedSteps = steps.Select((s, i) => s with { Index = i + 1 }).ToList();
        return new MigrationPlan
        {
            Name = name,
            Steps = indexedSteps,
            Status = PlanStatus.Ready
        };
    }

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

    private static MigrationStep CreateRenameNamespaceStep(string filePath, string newNamespace, string oldNamespace)
    {
        return new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.RenameNamespace,
            Source = filePath,
            Target = newNamespace,
            Metadata = new Dictionary<string, string> { [MetadataKeys.OldNamespace] = oldNamespace }
        };
    }

    #endregion
}
