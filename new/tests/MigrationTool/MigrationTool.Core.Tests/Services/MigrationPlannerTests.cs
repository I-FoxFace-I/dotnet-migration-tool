using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Services;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Services;

/// <summary>
/// Unit tests for MigrationPlanner - plans and validates migration operations.
/// </summary>
public class MigrationPlannerTests
{
    #region Test Constants

    private static class Paths
    {
        public const string SourceFile = @"C:\source.cs";
        public const string TargetFile = @"C:\target.cs";
        public const string Source1 = "source1.cs";
        public const string Source2 = "source2.cs";
        public const string Source3 = "source3.cs";
        public const string Target1 = "target1.cs";
        public const string Target2 = "target2.cs";
        public const string Target3 = "target3.cs";
        public const string Project = @"C:\project.csproj";
        public const string File = @"C:\file.cs";
    }

    private static class Namespaces
    {
        public const string Old = "OldNamespace";
        public const string New = "NewNamespace";
    }

    private static class PlanNames
    {
        public const string Test = "Test Plan";
        public const string Empty = "Empty Plan";
        public const string Imported = "Imported Plan";
        public const string WithMetadata = "Plan with Metadata";
        public const string Roundtrip = "Roundtrip Test";
    }

    private static class MetadataKeys
    {
        public const string OldNamespace = "OldNamespace";
        public const string PropertyName = "PropertyName";
        public const string Key = "key";
        public const string Value = "value";
    }

    private static class ValidationCodes
    {
        public const string EmptyPlan = "EMPTY_PLAN";
        public const string SourceNotFound = "SOURCE_NOT_FOUND";
        public const string TargetExists = "TARGET_EXISTS";
        public const string DuplicateTarget = "DUPLICATE_TARGET";
        public const string MissingOldNamespace = "MISSING_OLD_NAMESPACE";
        public const string MissingPropertyName = "MISSING_PROPERTY_NAME";
    }

    private static class TestPlans
    {
        private static readonly Guid TestPlanId = Guid.Parse("12345678-1234-1234-1234-123456789012");
        private static readonly DateTime TestCreatedAt = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime TestModifiedAt = new(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        private const string TestDescription = "Test Description";

        public static MigrationPlan SimplePlan => new()
        {
            Id = TestPlanId,
            Name = PlanNames.Imported,
            Description = TestDescription,
            CreatedAt = TestCreatedAt,
            ModifiedAt = TestModifiedAt,
            Steps =
            [
                new MigrationStep
                {
                    Index = 1,
                    Action = MigrationAction.MoveFile,
                    Source = "source.cs",
                    Target = "target.cs",
                    Metadata = new Dictionary<string, string>()
                }
            ]
        };

        public static MigrationPlan PlanWithMetadata => new()
        {
            Id = TestPlanId,
            Name = PlanNames.WithMetadata,
            CreatedAt = TestCreatedAt,
            ModifiedAt = TestModifiedAt,
            Steps =
            [
                new MigrationStep
                {
                    Index = 1,
                    Action = MigrationAction.RenameNamespace,
                    Source = "file.cs",
                    Target = Namespaces.New,
                    Metadata = new Dictionary<string, string> { [MetadataKeys.OldNamespace] = Namespaces.Old }
                }
            ]
        };

        public static string SerializeToJson(MigrationPlan plan) =>
            JsonSerializer.Serialize(plan);
    }

    #endregion

    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly Mock<ILogger<MigrationPlanner>> _loggerMock;

    public MigrationPlannerTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _loggerMock = new Mock<ILogger<MigrationPlanner>>();
    }

    private MigrationPlanner CreatePlanner()
    {
        return new MigrationPlanner(_fileSystemMock.Object, _loggerMock.Object);
    }

    #region CreatePlan Tests

    [Fact]
    public void CreatePlan_WithName_ReturnsNewPlan()
    {
        // Arrange
        var planner = CreatePlanner();

        // Act
        var plan = planner.CreatePlan(PlanNames.Test);

        // Assert
        plan.Should().NotBeNull();
        plan.Name.Should().Be(PlanNames.Test);
        plan.Status.Should().Be(PlanStatus.Draft);
        plan.Steps.Should().BeEmpty();
        plan.Id.Should().NotBeEmpty();
        plan.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreatePlan_GeneratesUniqueIds()
    {
        // Arrange
        var planner = CreatePlanner();

        // Act
        var plan1 = planner.CreatePlan("Plan 1");
        var plan2 = planner.CreatePlan("Plan 2");

        // Assert
        plan1.Id.Should().NotBe(plan2.Id);
    }

    #endregion

    #region AddStep Tests

    [Fact]
    public void AddStep_ToEmptyPlan_AddsStepWithIndex1()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(PlanNames.Test);
        var step = CreateMoveFileStep(Paths.Source1, Paths.Target1);

        // Act
        var updatedPlan = planner.AddStep(plan, step);

        // Assert
        updatedPlan.Steps.Should().HaveCount(1);
        updatedPlan.Steps[0].Index.Should().Be(1);
        updatedPlan.Steps[0].Source.Should().Be(Paths.Source1);
        updatedPlan.ModifiedAt.Should().BeAfter(plan.ModifiedAt);
    }

    [Fact]
    public void AddStep_ToExistingPlan_IncrementsIndex()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(PlanNames.Test);
        var step1 = CreateMoveFileStep(Paths.Source1, Paths.Target1);
        var step2 = CreateMoveFileStep(Paths.Source2, Paths.Target2);

        // Act
        var planWith1 = planner.AddStep(plan, step1);
        var planWith2 = planner.AddStep(planWith1, step2);

        // Assert
        planWith2.Steps.Should().HaveCount(2);
        planWith2.Steps[0].Index.Should().Be(1);
        planWith2.Steps[1].Index.Should().Be(2);
    }

    [Fact]
    public void AddStep_PreservesOriginalPlan()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(PlanNames.Test);
        var step = CreateMoveFileStep(Paths.Source1, Paths.Target1);

        // Act
        var updatedPlan = planner.AddStep(plan, step);

        // Assert
        plan.Steps.Should().BeEmpty();
        updatedPlan.Steps.Should().HaveCount(1);
    }

    #endregion

    #region RemoveStep Tests

    [Fact]
    public void RemoveStep_WithValidIndex_RemovesStep()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(
            CreateMoveFileStep(Paths.Source1, Paths.Target1),
            CreateMoveFileStep(Paths.Source2, Paths.Target2),
            CreateMoveFileStep(Paths.Source3, Paths.Target3));

        // Act
        var updatedPlan = planner.RemoveStep(plan, 1); // Remove middle step

        // Assert
        updatedPlan.Steps.Should().HaveCount(2);
        updatedPlan.Steps[0].Source.Should().Be(Paths.Source1);
        updatedPlan.Steps[1].Source.Should().Be(Paths.Source3);
    }

    [Fact]
    public void RemoveStep_ReindexesRemainingSteps()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(
            CreateMoveFileStep(Paths.Source1, Paths.Target1),
            CreateMoveFileStep(Paths.Source2, Paths.Target2),
            CreateMoveFileStep(Paths.Source3, Paths.Target3));

        // Act
        var updatedPlan = planner.RemoveStep(plan, 0); // Remove first step

        // Assert
        updatedPlan.Steps[0].Index.Should().Be(1);
        updatedPlan.Steps[1].Index.Should().Be(2);
    }

    [Fact]
    public void RemoveStep_WithNegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(CreateMoveFileStep(Paths.Source1, Paths.Target1));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => planner.RemoveStep(plan, -1));
    }

    [Fact]
    public void RemoveStep_WithIndexOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(CreateMoveFileStep(Paths.Source1, Paths.Target1));

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => planner.RemoveStep(plan, 5));
    }

    #endregion

    #region ValidatePlanAsync Tests

    [Fact]
    public async Task ValidatePlanAsync_WithEmptyPlan_ReturnsError()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(PlanNames.Empty);

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationCodes.EmptyPlan);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithValidPlan_ReturnsSuccess()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(CreateMoveFileStep(Paths.SourceFile, Paths.TargetFile));

        SetupFileExists(Paths.SourceFile, true);
        SetupFileExists(Paths.TargetFile, false);

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidatePlanAsync_WithMissingSource_ReturnsWarning()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(CreateMoveFileStep(Paths.SourceFile, Paths.TargetFile));

        SetupFileExists(Paths.SourceFile, false);
        SetupFileExists(Paths.TargetFile, false);

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeTrue(); // Warnings don't make it invalid
        result.Warnings.Should().Contain(w => w.Code == ValidationCodes.SourceNotFound);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithExistingTarget_ReturnsWarning()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(CreateMoveFileStep(Paths.SourceFile, Paths.TargetFile));

        SetupFileExists(Paths.SourceFile, true);
        SetupFileExists(Paths.TargetFile, true);

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.Warnings.Should().Contain(w => w.Code == ValidationCodes.TargetExists);
    }

    [Fact]
    public async Task ValidatePlanAsync_WithDuplicateTargets_ReturnsError()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(
            CreateMoveFileStep(Paths.Source1, Paths.TargetFile),
            CreateMoveFileStep(Paths.Source2, Paths.TargetFile)); // Same target

        SetupFileExists(Paths.Source1, true);
        SetupFileExists(Paths.Source2, true);
        SetupFileExists(Paths.TargetFile, false);

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationCodes.DuplicateTarget);
    }

    [Fact]
    public async Task ValidatePlanAsync_RenameNamespaceWithoutOldNamespace_ReturnsError()
    {
        // Arrange
        var planner = CreatePlanner();
        var step = new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.RenameNamespace,
            Source = Paths.File,
            Target = Namespaces.New,
            Metadata = new Dictionary<string, string>() // Missing OldNamespace
        };
        var plan = new MigrationPlan { Name = PlanNames.Test, Steps = [step] };

        SetupFileExists(Paths.File, true);

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationCodes.MissingOldNamespace);
    }

    [Fact]
    public async Task ValidatePlanAsync_UpdatePropertyWithoutPropertyName_ReturnsError()
    {
        // Arrange
        var planner = CreatePlanner();
        var step = new MigrationStep
        {
            Index = 1,
            Action = MigrationAction.UpdateProjectProperty,
            Source = Paths.Project,
            Target = "net9.0",
            Metadata = new Dictionary<string, string>() // Missing PropertyName
        };
        var plan = new MigrationPlan { Name = PlanNames.Test, Steps = [step] };

        // Act
        var result = await planner.ValidatePlanAsync(plan);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == ValidationCodes.MissingPropertyName);
    }

    #endregion

    #region ExportPlan Tests

    [Fact]
    public void ExportPlan_ReturnsValidJson()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = CreatePlanWithSteps(
            CreateMoveFileStep(Paths.Source1, Paths.Target1),
            CreateMoveFileStep(Paths.Source2, Paths.Target2));

        // Act
        var json = planner.ExportPlan(plan);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"name\":");
        json.Should().Contain("\"steps\":");
        json.Should().Contain(Paths.Source1);
        json.Should().Contain("MoveFile");
    }

    [Fact]
    public void ExportPlan_PreservesAllFields()
    {
        // Arrange
        var planner = CreatePlanner();
        var plan = new MigrationPlan
        {
            Name = PlanNames.Test,
            Description = "Test Description",
            Steps = [new MigrationStep
            {
                Index = 1,
                Action = MigrationAction.RenameNamespace,
                Source = Paths.File,
                Target = Namespaces.New,
                Metadata = new Dictionary<string, string> { [MetadataKeys.OldNamespace] = Namespaces.Old }
            }]
        };

        // Act
        var json = planner.ExportPlan(plan);

        // Assert
        json.Should().Contain(PlanNames.Test);
        json.Should().Contain("Test Description");
        json.Should().Contain("RenameNamespace");
        json.Should().Contain(Namespaces.Old);
    }

    #endregion

    #region ImportPlan Tests

    [Fact]
    public void ImportPlan_WithValidJson_ReturnsPlan()
    {
        // Arrange
        var planner = CreatePlanner();
        var json = TestPlans.SerializeToJson(TestPlans.SimplePlan);

        // Act
        var plan = planner.ImportPlan(json);

        // Assert
        plan.Should().NotBeNull();
        plan.Name.Should().Be(PlanNames.Imported);
        plan.Description.Should().Be("Test Description");
        plan.Steps.Should().HaveCount(1);
        plan.Steps[0].Action.Should().Be(MigrationAction.MoveFile);
        plan.Steps[0].Source.Should().Be("source.cs");
        plan.Status.Should().Be(PlanStatus.Draft);
    }

    [Fact]
    public void ImportPlan_PreservesMetadata()
    {
        // Arrange
        var planner = CreatePlanner();
        var json = TestPlans.SerializeToJson(TestPlans.PlanWithMetadata);

        // Act
        var plan = planner.ImportPlan(json);

        // Assert
        plan.Steps[0].Metadata.Should().ContainKey(MetadataKeys.OldNamespace);
        plan.Steps[0].Metadata[MetadataKeys.OldNamespace].Should().Be(Namespaces.Old);
    }

    [Fact]
    public void ImportPlan_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var planner = CreatePlanner();
        const string invalidJson = "{ invalid json }";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => planner.ImportPlan(invalidJson));
    }

    [Fact]
    public void ExportThenImport_RoundTrips()
    {
        // Arrange
        var planner = CreatePlanner();
        var originalPlan = new MigrationPlan
        {
            Name = PlanNames.Roundtrip,
            Description = "Testing export/import",
            Steps = [
                new MigrationStep
                {
                    Index = 1,
                    Action = MigrationAction.MoveFolder,
                    Source = "src/folder",
                    Target = "dest/folder",
                    Metadata = new Dictionary<string, string> { [MetadataKeys.Key] = MetadataKeys.Value }
                }
            ]
        };

        // Act
        var json = planner.ExportPlan(originalPlan);
        var importedPlan = planner.ImportPlan(json);

        // Assert
        importedPlan.Name.Should().Be(originalPlan.Name);
        importedPlan.Description.Should().Be(originalPlan.Description);
        importedPlan.Steps.Should().HaveCount(1);
        importedPlan.Steps[0].Action.Should().Be(MigrationAction.MoveFolder);
        importedPlan.Steps[0].Source.Should().Be("src/folder");
        importedPlan.Steps[0].Metadata[MetadataKeys.Key].Should().Be(MetadataKeys.Value);
    }

    #endregion

    #region Helper Methods

    private static MigrationStep CreateMoveFileStep(string source, string target)
    {
        return new MigrationStep
        {
            Index = 0, // Will be set by AddStep
            Action = MigrationAction.MoveFile,
            Source = source,
            Target = target
        };
    }

    private MigrationPlan CreatePlanWithSteps(params MigrationStep[] steps)
    {
        var planner = CreatePlanner();
        var plan = planner.CreatePlan(PlanNames.Test);

        foreach (var step in steps)
        {
            plan = planner.AddStep(plan, step);
        }

        return plan;
    }

    private void SetupFileExists(string path, bool exists)
    {
        _fileSystemMock.Setup(x => x.ExistsAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
    }

    #endregion
}
