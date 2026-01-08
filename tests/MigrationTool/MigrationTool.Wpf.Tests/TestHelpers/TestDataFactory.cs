using System.IO;
using MigrationTool.Core.Abstractions.Models;

namespace MigrationTool.Wpf.Tests.TestHelpers;

/// <summary>
/// Factory for creating test data objects.
/// </summary>
public static class TestDataFactory
{
    #region Constants

    public static class SolutionNames
    {
        public const string Default = "TestSolution";
        public const string Empty = "EmptySolution";
        public const string TestOnly = "TestOnlySolution";
        public const string Large = "LargeSolution";
    }

    public static class Paths
    {
        public const string SolutionFile = @"C:\test\solution.sln";
        public const string ProjectsRoot = @"C:\test";
    }

    public static class ProjectNames
    {
        public const string Proj1 = "Proj1";
        public const string Proj2 = "Proj2";
        public const string Proj1Tests = "Proj1.Tests";
        public const string Proj2Tests = "Proj2.Tests";
    }

    public static class Counts
    {
        public const int DefaultFileCount = 10;
        public const int DefaultClassCount = 5;
        public const int DefaultTestCount = 20;
    }

    #endregion

    #region Project Factory

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

    #endregion

    #region Solution Factory

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

    public static SolutionInfo CreateLargeSolution(int projectCount = 10)
    {
        var projects = new List<ProjectInfo>();
        for (int i = 0; i < projectCount; i++)
        {
            var isTest = i % 3 == 0;
            projects.Add(CreateProject($"Project{i}", isTest, fileCount: 20, classCount: 10, testCount: isTest ? 50 : 0));
        }
        return CreateSolution(SolutionNames.Large, projects.ToArray());
    }

    #endregion

    #region Migration Plan Factory

    public static MigrationPlan CreateEmptyPlan(string name = "Test Plan")
    {
        return new MigrationPlan { Name = name };
    }

    public static MigrationPlan CreatePlanWithSteps(string name = "Test Plan", int stepCount = 3)
    {
        var steps = new List<MigrationStep>();
        for (int i = 0; i < stepCount; i++)
        {
            steps.Add(new MigrationStep
            {
                Index = i + 1,
                Action = MigrationAction.MoveFile,
                Source = $@"C:\source\file{i}.cs",
                Target = $@"C:\target\file{i}.cs"
            });
        }

        return new MigrationPlan
        {
            Name = name,
            Steps = steps
        };
    }

    #endregion
}
