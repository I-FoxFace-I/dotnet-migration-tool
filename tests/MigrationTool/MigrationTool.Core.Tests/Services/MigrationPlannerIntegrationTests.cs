using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Services;
using MigrationTool.Tests.Infrastructure.Fixtures;
using MigrationTool.Tests.Infrastructure.Generators;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Services;

/// <summary>
/// Integration tests for MigrationPlanner using real file system operations.
/// Each test creates a fake .NET solution using FakeSolutionGenerator.
/// TempDirectoryFixture creates a unique directory (with GUID) for each test and cleans up on dispose.
/// </summary>
public class MigrationPlannerIntegrationTests : IDisposable
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
        public const string Entities = "Entities";
        public const string Moved = "Moved";
        public const string Models = "Models";
    }

    private static class FileNames
    {
        public const string Customer = "Customer.cs";
        public const string Product = "Product.cs";
        public const string NonExistent = "NonExistent.cs";
        public const string Target = "Target.cs";
        public const string PlanJson = "migration-plan.json";
    }

    private static class FilePatterns
    {
        public const string CSharpFiles = "*.cs";
    }

    private static class Namespaces
    {
        public const string OldDomain = "TestApp.Domain.Entities";
        public const string NewCore = "TestApp.Core.Models";

        public static string GetOld(string solutionName) => $"{solutionName}.Domain.Entities";
        public static string GetNew(string solutionName) => $"{solutionName}.Core.Models";
    }

    private static class PlanNames
    {
        public const string Test = "Test Plan";
        public const string Validation = "Validation Test Plan";
        public const string Export = "Export Test Plan";
    }

    private static class MetadataKeys
    {
        public const string OldNamespace = "OldNamespace";
    }

    private static class ValidationCodes
    {
        public const string SourceNotFound = "SOURCE_NOT_FOUND";
        public const string TargetExists = "TARGET_EXISTS";
        public const string DuplicateTarget = "DUPLICATE_TARGET";
        public const string MissingOldNamespace = "MISSING_OLD_NAMESPACE";
    }

    private static class ActionNames
    {
        public const string MoveFile = "MoveFile";
        public const string RenameNamespace = "RenameNamespace";
    }

    #endregion

    private readonly TempDirectoryFixture _tempDir;
    private readonly LocalFileSystemService _fileSystem;
    private readonly MigrationPlanner _planner;

    public MigrationPlannerIntegrationTests()
    {
        _tempDir = new TempDirectoryFixture();
        _fileSystem = new LocalFileSystemService();
        _planner = new MigrationPlanner(_fileSystem, Mock.Of<ILogger<MigrationPlanner>>());
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

    #region Validation Integration Tests

    [Fact]
    public async Task ValidatePlanAsync_WithExistingEntityFile_ReturnsNoWarnings()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var plan = CreatePlan(PlanNames.Validation,
            CreateMoveFileStep(sourceFile, targetFile));

        // Act
        var result = await _planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().NotContain(w => w.Code == ValidationCodes.SourceNotFound);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithNonExistentSourceFile_ReturnsWarning()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var nonExistentFile = Path.Combine(GetProjectDirectory(domainProject), FileNames.NonExistent);
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.NonExistent);

        var plan = CreatePlan(PlanNames.Validation,
            CreateMoveFileStep(nonExistentFile, targetFile));

        // Act
        var result = await _planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeTrue(); // Warnings don't invalidate
        result.Warnings.Should().Contain(w => w.Code == ValidationCodes.SourceNotFound);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithExistingTargetFile_ReturnsWarning()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        // Both Customer.cs and Product.cs exist, so moving Customer to Product's location should warn
        var sourceFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Product));

        var plan = CreatePlan(PlanNames.Validation,
            CreateMoveFileStep(sourceFile, targetFile));

        // Act
        var result = await _planner.ValidatePlanAsync(plan);

        // Assert
        result.Warnings.Should().Contain(w => w.Code == ValidationCodes.TargetExists);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithProjectFolder_ValidatesCorrectly()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFolder = GetProjectDirectory(domainProject);
        var targetFolder = GetMovedProjectPath(SolutionNames.Default, ProjectSuffixes.Domain);

        var plan = CreatePlan(PlanNames.Validation,
            CreateMoveFolderStep(sourceFolder, targetFolder));

        // Act
        var result = await _planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().NotContain(w => w.Code == ValidationCodes.SourceNotFound);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithRenameNamespaceAndMissingMetadata_ReturnsError()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entityFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));

        var step = new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.RenameNamespace,
            Source = entityFile,
            Target = Namespaces.GetNew(SolutionNames.Default),
            Metadata = new Dictionary<string, string>() // Missing OldNamespace
        };

        var plan = new MigrationPlan
        {
            Name = PlanNames.Validation,
            Steps = [step]
        };

        // Act
        var result = await _planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationCodes.MissingOldNamespace);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithDuplicateTargets_ReturnsError()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var productFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Product));
        var sameTarget = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Target);

        var plan = CreatePlan(PlanNames.Validation,
            CreateMoveFileStep(customerFile, sameTarget),
            CreateMoveFileStep(productFile, sameTarget)); // Same target!

        // Act
        var result = await _planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationCodes.DuplicateTarget);
    }

    #endregion

    #region Export/Import Integration Tests

    [Fact]
    public async Task ExportPlan_ThenSaveToFile_CreatesValidJsonFile()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var plan = CreatePlan(PlanNames.Export,
            CreateMoveFileStep(customerFile, targetFile),
            CreateRenameNamespaceStep(targetFile, Namespaces.GetNew(SolutionNames.Default), Namespaces.GetOld(SolutionNames.Default)));

        // Act
        var json = _planner.ExportPlan(plan);
        var filePath = _tempDir.CreateFile(FileNames.PlanJson, json);

        // Assert
        File.Exists(filePath).Should().BeTrue();
        var savedContent = await File.ReadAllTextAsync(filePath);
        savedContent.Should().Contain(PlanNames.Export);
        savedContent.Should().Contain(ActionNames.MoveFile);
        savedContent.Should().Contain(ActionNames.RenameNamespace);
        savedContent.Should().Contain(FileNames.Customer);
    }

    [Fact]
    public async Task ImportPlan_FromSavedFile_RestoresPlanWithCorrectPaths()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var originalPlan = CreatePlan(PlanNames.Export,
            CreateMoveFileStep(customerFile, targetFile),
            CreateRenameNamespaceStep(targetFile, Namespaces.GetNew(SolutionNames.Default), Namespaces.GetOld(SolutionNames.Default)));

        var json = _planner.ExportPlan(originalPlan);
        var filePath = _tempDir.CreateFile(FileNames.PlanJson, json);

        // Act
        var loadedJson = await File.ReadAllTextAsync(filePath);
        var importedPlan = _planner.ImportPlan(loadedJson);

        // Assert
        importedPlan.Name.Should().Be(originalPlan.Name);
        importedPlan.Steps.Should().HaveCount(2);
        importedPlan.Steps[0].Action.Should().Be(MigrationAction.MoveFile);
        importedPlan.Steps[0].Source.Should().Be(customerFile);
        importedPlan.Steps[1].Action.Should().Be(MigrationAction.RenameNamespace);
        importedPlan.Steps[1].Metadata[MetadataKeys.OldNamespace].Should().Be(Namespaces.GetOld(SolutionNames.Default));
    }

    [Fact]
    public void ExportImport_WithProjectFolderPaths_PreservesAbsolutePaths()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var sourceFolder = GetProjectDirectory(domainProject);
        var targetFolder = GetMovedProjectPath(SolutionNames.Default, ProjectSuffixes.Domain);

        var originalPlan = CreatePlan(PlanNames.Export,
            CreateMoveFolderStep(sourceFolder, targetFolder));

        // Act
        var json = _planner.ExportPlan(originalPlan);
        var importedPlan = _planner.ImportPlan(json);

        // Assert
        importedPlan.Steps[0].Source.Should().Be(sourceFolder);
        importedPlan.Steps[0].Target.Should().Be(targetFolder);
    }

    [Fact]
    public async Task ExportImport_RoundTrip_CanBeValidated()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var customerFile = domainProject.SourceFiles.First(f => f.EndsWith(FileNames.Customer));
        var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, FileNames.Customer);

        var originalPlan = CreatePlan(PlanNames.Export,
            CreateMoveFileStep(customerFile, targetFile));

        // Act
        var json = _planner.ExportPlan(originalPlan);
        var importedPlan = _planner.ImportPlan(json);
        var validationResult = await _planner.ValidatePlanAsync(importedPlan);

        // Assert
        validationResult.IsValid.Should().BeTrue();
        validationResult.Warnings.Should().NotContain(w => w.Code == ValidationCodes.SourceNotFound);
    }

    #endregion

    #region Plan Building Integration Tests

    [Fact]
    public void CreatePlan_AndAddStepsForMultipleEntities_MaintainsCorrectOrder()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entityFiles = domainProject.SourceFiles
            .Where(f => f.Contains(FolderNames.Entities))
            .Take(3)
            .ToList();

        // Act
        var plan = _planner.CreatePlan(PlanNames.Test);
        foreach (var entityFile in entityFiles)
        {
            var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, Path.GetFileName(entityFile));
            plan = _planner.AddStep(plan, CreateMoveFileStep(entityFile, targetFile));
        }

        // Assert
        plan.Steps.Should().HaveCount(3);
        plan.Steps[0].Index.Should().Be(1);
        plan.Steps[1].Index.Should().Be(2);
        plan.Steps[2].Index.Should().Be(3);
        plan.Steps.Select(s => s.Source).Should().BeEquivalentTo(entityFiles);
    }

    [Fact]
    public void RemoveStep_FromMiddle_ReindexesCorrectly()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entityFiles = domainProject.SourceFiles
            .Where(f => f.Contains(FolderNames.Entities))
            .Take(3)
            .ToList();

        var plan = _planner.CreatePlan(PlanNames.Test);
        foreach (var entityFile in entityFiles)
        {
            var targetFile = GetFileInFolder(domainProject, FolderNames.Moved, Path.GetFileName(entityFile));
            plan = _planner.AddStep(plan, CreateMoveFileStep(entityFile, targetFile));
        }

        // Act
        plan = _planner.RemoveStep(plan, 1); // Remove middle step

        // Assert
        plan.Steps.Should().HaveCount(2);
        plan.Steps[0].Index.Should().Be(1);
        plan.Steps[0].Source.Should().Be(entityFiles[0]);
        plan.Steps[1].Index.Should().Be(2);
        plan.Steps[1].Source.Should().Be(entityFiles[2]);
    }

    [Fact]
    public async Task CreateComplexPlan_WithMoveAndRename_BuildsPlanCorrectly()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entitiesFolder = GetSubfolder(domainProject, FolderNames.Entities);
        var modelsFolder = GetSubfolder(domainProject, FolderNames.Models);

        var entityFiles = Directory.GetFiles(entitiesFolder, FilePatterns.CSharpFiles);

        // Act - Create a plan that moves folder and renames namespaces
        var plan = _planner.CreatePlan(PlanNames.Test);
        plan = _planner.AddStep(plan, CreateMoveFolderStep(entitiesFolder, modelsFolder));

        foreach (var entityFile in entityFiles)
        {
            var movedFile = Path.Combine(modelsFolder, Path.GetFileName(entityFile));
            plan = _planner.AddStep(plan, CreateRenameNamespaceStep(
                movedFile,
                Namespaces.GetNew(SolutionNames.Default),
                Namespaces.GetOld(SolutionNames.Default)));
        }

        // Assert - Plan is built correctly (validation would show warnings for non-existent targets after move)
        plan.Steps.Count.Should().BeGreaterThan(1);
        plan.Steps[0].Action.Should().Be(MigrationAction.MoveFolder);
        plan.Steps[0].Source.Should().Be(entitiesFolder);
        plan.Steps[0].Target.Should().Be(modelsFolder);

        // All subsequent steps should be RenameNamespace
        plan.Steps.Skip(1).Should().OnlyContain(s => s.Action == MigrationAction.RenameNamespace);
        plan.Steps.Skip(1).Should().OnlyContain(s => s.Metadata.ContainsKey(MetadataKeys.OldNamespace));
    }

    [Fact]
    public async Task ValidatePlanAsync_WithExistingEntitiesFolder_ReturnsValid()
    {
        // Arrange
        var solution = FakeSolutionGenerator.CreateEfCoreSolution(_tempDir, SolutionNames.Default);
        var domainProject = solution.Projects.First(p => p.Name.EndsWith(ProjectSuffixes.Domain));

        var entitiesFolder = GetSubfolder(domainProject, FolderNames.Entities);
        var modelsFolder = GetSubfolder(domainProject, FolderNames.Models);

        var plan = CreatePlan(PlanNames.Validation,
            CreateMoveFolderStep(entitiesFolder, modelsFolder));

        // Act
        var validationResult = await _planner.ValidatePlanAsync(plan);

        // Assert
        validationResult.IsValid.Should().BeTrue();
        validationResult.Warnings.Should().NotContain(w => w.Code == ValidationCodes.SourceNotFound);
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
