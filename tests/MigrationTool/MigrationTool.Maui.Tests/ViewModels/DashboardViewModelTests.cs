using FluentAssertions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Localization;
using Moq;
using Xunit;

namespace MigrationTool.Maui.Tests.ViewModels;

/// <summary>
/// Unit tests for DashboardViewModel - displays solution statistics.
/// Uses TestableDashboardViewModel to avoid MAUI framework dependencies.
/// </summary>
public class DashboardViewModelTests
{
    #region Test Constants

    private static class SolutionNames
    {
        public const string Default = "TestSolution";
        public const string Empty = "EmptySolution";
        public const string TestOnly = "TestOnlySolution";
    }

    private static class Paths
    {
        public const string SolutionFile = @"C:\test\solution.sln";
        public const string ProjectsRoot = @"C:\test";
    }

    private static class ProjectNames
    {
        public const string Proj1 = "Proj1";
        public const string Proj2 = "Proj2";
        public const string Proj1Tests = "Proj1.Tests";
        public const string Proj2Tests = "Proj2.Tests";
    }

    private static class LocalizationKeys
    {
        public const string DashboardTitle = "DashboardTitle";
    }

    private static class Counts
    {
        public const int DefaultFileCount = 10;
        public const int DefaultClassCount = 5;
        public const int DefaultTestCount = 20;
    }

    private static class PropertyNames
    {
        public const string Solution = "Solution";
        public const string ProjectCount = "ProjectCount";
        public const string TestProjectCount = "TestProjectCount";
        public const string SourceProjectCount = "SourceProjectCount";
        public const string FileCount = "FileCount";
        public const string ClassCount = "ClassCount";
        public const string TestCount = "TestCount";
    }

    #endregion

    #region Test Helpers

    private static class TestHelpers
    {
        public static ProjectInfo CreateProject(
            string name,
            bool isTest,
            int fileCount = Counts.DefaultFileCount,
            int classCount = Counts.DefaultClassCount,
            int testCount = 0)
        {
            var sourceFiles = new List<SourceFileInfo>();
            for (int i = 0; i < fileCount; i++)
            {
                var classes = new List<TypeInfo>();
                if (i < classCount)
                {
                    var tests = new List<TestInfo>();
                    if (isTest && i == 0 && testCount > 0)
                    {
                        for (int t = 0; t < testCount; t++)
                        {
                            tests.Add(new TestInfo { Name = $"Test{t}" });
                        }
                    }
                    classes.Add(new TypeInfo { Name = $"Class{i}", Tests = tests });
                }
                sourceFiles.Add(new SourceFileInfo
                {
                    Name = $"File{i}.cs",
                    Path = Path.Combine(Paths.ProjectsRoot, name, $"File{i}.cs"),
                    Classes = classes
                });
            }

            return new ProjectInfo
            {
                Name = name,
                Path = Path.Combine(Paths.ProjectsRoot, name, $"{name}.csproj"),
                IsTestProject = isTest,
                TargetFramework = "net9.0",
                SourceFiles = sourceFiles
            };
        }

        public static SolutionInfo CreateSolution(string name, params ProjectInfo[] projects)
        {
            return new SolutionInfo
            {
                Name = name,
                Path = Paths.SolutionFile,
                Projects = projects.ToList()
            };
        }

        public static SolutionInfo CreateDefaultSolution()
        {
            return CreateSolution(SolutionNames.Default,
                CreateProject(ProjectNames.Proj1, isTest: false, fileCount: 10, classCount: 5, testCount: 0),
                CreateProject(ProjectNames.Proj2, isTest: false, fileCount: 15, classCount: 8, testCount: 0),
                CreateProject(ProjectNames.Proj1Tests, isTest: true, fileCount: 5, classCount: 3, testCount: 20));
        }

        public static SolutionInfo CreateEmptySolution()
        {
            return new SolutionInfo
            {
                Name = SolutionNames.Empty,
                Path = Paths.SolutionFile,
                Projects = []
            };
        }

        public static SolutionInfo CreateTestOnlySolution()
        {
            return CreateSolution(SolutionNames.TestOnly,
                CreateProject(ProjectNames.Proj1Tests, isTest: true, fileCount: 5, classCount: 3, testCount: 10),
                CreateProject(ProjectNames.Proj2Tests, isTest: true, fileCount: 8, classCount: 4, testCount: 15));
        }
    }

    #endregion

    private readonly Mock<ILocalizationService> _localizationMock;

    public DashboardViewModelTests()
    {
        _localizationMock = new Mock<ILocalizationService>();
        _localizationMock.Setup(x => x.Get(It.IsAny<string>())).Returns((string key) => key);
    }

    private TestableDashboardViewModel CreateViewModel()
    {
        return new TestableDashboardViewModel(_localizationMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsTitle()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Title.Should().NotBeNullOrEmpty();
        viewModel.Title.Should().Be(LocalizationKeys.DashboardTitle);
    }

    [Fact]
    public void Constructor_InitializesWithZeroCounts()
    {
        // Arrange & Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.ProjectCount.Should().Be(0);
        viewModel.TestProjectCount.Should().Be(0);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.FileCount.Should().Be(0);
        viewModel.ClassCount.Should().Be(0);
        viewModel.TestCount.Should().Be(0);
        viewModel.Solution.Should().BeNull();
    }

    #endregion

    #region UpdateFromSolution Tests

    [Fact]
    public void UpdateFromSolution_WithValidSolution_UpdatesCounts()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestHelpers.CreateDefaultSolution();

        // Act
        viewModel.UpdateFromSolution(solution);

        // Assert
        viewModel.Solution.Should().Be(solution);
        viewModel.ProjectCount.Should().Be(3);
        viewModel.TestProjectCount.Should().Be(1);
        viewModel.SourceProjectCount.Should().Be(2);
        viewModel.FileCount.Should().Be(30); // 10 + 15 + 5
        viewModel.ClassCount.Should().Be(16); // 5 + 8 + 3
        viewModel.TestCount.Should().Be(20);
    }

    [Fact]
    public void UpdateFromSolution_WithNullSolution_ResetsToZero()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.UpdateFromSolution(TestHelpers.CreateDefaultSolution());

        // Act
        viewModel.UpdateFromSolution(null);

        // Assert
        viewModel.Solution.Should().BeNull();
        viewModel.ProjectCount.Should().Be(0);
        viewModel.TestProjectCount.Should().Be(0);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.FileCount.Should().Be(0);
        viewModel.ClassCount.Should().Be(0);
        viewModel.TestCount.Should().Be(0);
    }

    [Fact]
    public void UpdateFromSolution_WithEmptyProjects_SetsZeroCounts()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestHelpers.CreateEmptySolution();

        // Act
        viewModel.UpdateFromSolution(solution);

        // Assert
        viewModel.Solution.Should().Be(solution);
        viewModel.ProjectCount.Should().Be(0);
        viewModel.TestProjectCount.Should().Be(0);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.FileCount.Should().Be(0);
        viewModel.ClassCount.Should().Be(0);
        viewModel.TestCount.Should().Be(0);
    }

    [Fact]
    public void UpdateFromSolution_WithOnlyTestProjects_CountsCorrectly()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var solution = TestHelpers.CreateTestOnlySolution();

        // Act
        viewModel.UpdateFromSolution(solution);

        // Assert
        viewModel.ProjectCount.Should().Be(2);
        viewModel.TestProjectCount.Should().Be(2);
        viewModel.SourceProjectCount.Should().Be(0);
        viewModel.TestCount.Should().Be(25); // 10 + 15
    }

    #endregion

    #region Property Change Notification Tests

    [Fact]
    public void UpdateFromSolution_RaisesPropertyChangedEvents()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        var solution = TestHelpers.CreateDefaultSolution();

        // Act
        viewModel.UpdateFromSolution(solution);

        // Assert
        changedProperties.Should().Contain(PropertyNames.Solution);
        changedProperties.Should().Contain(PropertyNames.ProjectCount);
        changedProperties.Should().Contain(PropertyNames.FileCount);
        changedProperties.Should().Contain(PropertyNames.ClassCount);
        changedProperties.Should().Contain(PropertyNames.TestProjectCount);
        changedProperties.Should().Contain(PropertyNames.SourceProjectCount);
        changedProperties.Should().Contain(PropertyNames.TestCount);
    }

    [Fact]
    public void UpdateFromSolution_WithNull_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.UpdateFromSolution(TestHelpers.CreateDefaultSolution());

        var changedProperties = new List<string>();
        viewModel.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        viewModel.UpdateFromSolution(null);

        // Assert
        changedProperties.Should().Contain(PropertyNames.Solution);
        changedProperties.Should().Contain(PropertyNames.ProjectCount);
    }

    #endregion
}

/// <summary>
/// Testable version of DashboardViewModel - replicates ViewModel logic without MAUI dependencies.
/// </summary>
public class TestableDashboardViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly ILocalizationService _localization;

    private string _title = string.Empty;
    private SolutionInfo? _solution;
    private int _projectCount;
    private int _testProjectCount;
    private int _sourceProjectCount;
    private int _fileCount;
    private int _classCount;
    private int _testCount;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public SolutionInfo? Solution
    {
        get => _solution;
        set => SetProperty(ref _solution, value);
    }

    public int ProjectCount
    {
        get => _projectCount;
        set => SetProperty(ref _projectCount, value);
    }

    public int TestProjectCount
    {
        get => _testProjectCount;
        set => SetProperty(ref _testProjectCount, value);
    }

    public int SourceProjectCount
    {
        get => _sourceProjectCount;
        set => SetProperty(ref _sourceProjectCount, value);
    }

    public int FileCount
    {
        get => _fileCount;
        set => SetProperty(ref _fileCount, value);
    }

    public int ClassCount
    {
        get => _classCount;
        set => SetProperty(ref _classCount, value);
    }

    public int TestCount
    {
        get => _testCount;
        set => SetProperty(ref _testCount, value);
    }

    public TestableDashboardViewModel(ILocalizationService localization)
    {
        _localization = localization;
        Title = _localization.Get("DashboardTitle");
    }

    public void UpdateFromSolution(SolutionInfo? solution)
    {
        Solution = solution;

        if (solution == null)
        {
            ProjectCount = 0;
            TestProjectCount = 0;
            SourceProjectCount = 0;
            FileCount = 0;
            ClassCount = 0;
            TestCount = 0;
            return;
        }

        ProjectCount = solution.ProjectCount;
        TestProjectCount = solution.TestProjectCount;
        SourceProjectCount = solution.SourceProjectCount;
        FileCount = solution.Projects.Sum(p => p.FileCount);
        ClassCount = solution.Projects.Sum(p => p.ClassCount);
        TestCount = solution.Projects.Sum(p => p.TestCount);
    }
}
