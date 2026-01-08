using System.IO.Compression;
using System.Runtime.CompilerServices;
using Xunit;

namespace MigrationTool.Tests.Infrastructure.Fixtures;

/// <summary>
/// Provides access to real-world test projects from ZIP archives.
/// Extracts to datasets/test-fixtures/{testName}/ folder for easy debugging.
/// Folders are cleaned before extraction but not deleted after tests.
/// </summary>
public class TestProjectFixture : IDisposable
{
    private readonly string _extractDir;
    private bool _disposed;

    /// <summary>
    /// Path to the extracted project root directory.
    /// </summary>
    public string ProjectRoot { get; }

    /// <summary>
    /// Path to the solution file (if exists).
    /// </summary>
    public string? SolutionPath { get; private set; }

    /// <summary>
    /// Path to the main project file (.csproj) - useful when no solution exists.
    /// </summary>
    public string? MainProjectPath { get; private set; }

    /// <summary>
    /// Name of the test project.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Name of the test that created this fixture.
    /// </summary>
    public string TestName { get; }

    private TestProjectFixture(string projectName, string testName, string extractDir, string projectRoot)
    {
        ProjectName = projectName;
        TestName = testName;
        _extractDir = extractDir;
        ProjectRoot = projectRoot;
    }

    /// <summary>
    /// Creates a fixture from a ZIP archive in the datasets folder.
    /// Extracts to datasets/test-fixtures/{testName}/ folder.
    /// </summary>
    /// <param name="zipFileName">Name of the ZIP file (e.g., "Humanizer-slim.zip")</param>
    /// <param name="solutionOrProjectFileName">Optional: specific solution or project file name to look for</param>
    /// <param name="testName">Name of the test (auto-detected from caller)</param>
    public static TestProjectFixture FromZip(
        string zipFileName, 
        string? solutionOrProjectFileName = null,
        [CallerMemberName] string testName = "")
    {
        var zipPath = FindZipFile(zipFileName);
        if (zipPath == null)
        {
            throw new FileNotFoundException(
                $"Test fixture ZIP not found: {zipFileName}. " +
                $"Expected in datasets/test-fixtures/ folder.");
        }

        // Get test-fixtures directory (same as ZIP location)
        var testFixturesDir = Path.GetDirectoryName(zipPath)!;
        
        // Create extraction directory based on test name
        var sanitizedTestName = SanitizeDirectoryName(testName);
        var extractDir = Path.Combine(testFixturesDir, sanitizedTestName);

        // Clean existing directory if it exists
        if (Directory.Exists(extractDir))
        {
            try
            {
                Directory.Delete(extractDir, recursive: true);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Failed to clean extraction directory: {extractDir}. " +
                    $"Please close any applications using files in this directory.", ex);
            }
        }

        Directory.CreateDirectory(extractDir);

        // Extract ZIP
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        // Find the project root (usually the first directory inside the ZIP)
        var extractedDirs = Directory.GetDirectories(extractDir);
        var projectRoot = extractedDirs.Length == 1 
            ? extractedDirs[0] 
            : extractDir;

        var projectName = Path.GetFileNameWithoutExtension(zipFileName);
        var fixture = new TestProjectFixture(projectName, testName, extractDir, projectRoot);

        // Find solution or project file
        if (solutionOrProjectFileName?.EndsWith(".sln") == true)
        {
            fixture.SolutionPath = FindSolutionFile(projectRoot, solutionOrProjectFileName);
        }
        else if (solutionOrProjectFileName?.EndsWith(".csproj") == true)
        {
            fixture.MainProjectPath = FindProjectFile(projectRoot, solutionOrProjectFileName);
        }
        else
        {
            // Try to find solution first, then project
            fixture.SolutionPath = FindSolutionFile(projectRoot, null);
            if (fixture.SolutionPath == null)
            {
                fixture.MainProjectPath = FindMainProjectFile(projectRoot);
            }
        }

        return fixture;
    }

    /// <summary>
    /// Creates a Humanizer test fixture (slim version without tests/benchmarks).
    /// Note: Humanizer doesn't have a .sln file, so we use the main project.
    /// </summary>
    /// <param name="testName">Name of the test (auto-detected from caller)</param>
    public static TestProjectFixture Humanizer([CallerMemberName] string testName = "")
    {
        return FromZip("Humanizer-slim.zip", "Humanizer.csproj", testName);
    }

    /// <summary>
    /// Creates a full Humanizer test fixture (includes tests and benchmarks).
    /// </summary>
    /// <param name="testName">Name of the test (auto-detected from caller)</param>
    public static TestProjectFixture HumanizerFull([CallerMemberName] string testName = "")
    {
        return FromZip("Humanizer-main.zip", "Humanizer.csproj", testName);
    }

    /// <summary>
    /// Returns true if this fixture has a solution file.
    /// </summary>
    public bool HasSolution => SolutionPath != null;

    /// <summary>
    /// Returns true if this fixture has a main project file.
    /// </summary>
    public bool HasMainProject => MainProjectPath != null;

    /// <summary>
    /// Gets the best entry point for analysis (solution or main project).
    /// </summary>
    public string? EntryPoint => SolutionPath ?? MainProjectPath;

    /// <summary>
    /// Gets all C# files in the project.
    /// </summary>
    public IEnumerable<string> GetCSharpFiles()
    {
        return Directory.EnumerateFiles(ProjectRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                       !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    /// <summary>
    /// Gets all project files (.csproj) in the project.
    /// </summary>
    public IEnumerable<string> GetProjectFiles()
    {
        return Directory.EnumerateFiles(ProjectRoot, "*.csproj", SearchOption.AllDirectories);
    }

    /// <summary>
    /// Gets a specific file path within the project.
    /// </summary>
    public string GetFilePath(params string[] relativePath)
    {
        return Path.Combine(new[] { ProjectRoot }.Concat(relativePath).ToArray());
    }

    /// <summary>
    /// Checks if a file exists within the project.
    /// </summary>
    public bool FileExists(params string[] relativePath)
    {
        return File.Exists(GetFilePath(relativePath));
    }

    private static string SanitizeDirectoryName(string name)
    {
        // Replace invalid characters with underscore
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        
        // Limit length
        if (sanitized.Length > 100)
        {
            sanitized = sanitized[..100];
        }

        return sanitized;
    }

    private static string? FindZipFile(string zipFileName)
    {
        // Search in multiple possible locations
        var searchPaths = new[]
        {
            // From test execution directory - go up to find datasets
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "datasets", "test-fixtures", zipFileName),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "datasets", "test-fixtures", zipFileName),
            // From solution root
            FindFromSolutionRoot(zipFileName),
            // Direct path (for debugging)
            Path.Combine("datasets", "test-fixtures", zipFileName),
        };

        foreach (var searchPath in searchPaths.Where(p => p != null))
        {
            var fullPath = Path.GetFullPath(searchPath!);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    private static string? FindFromSolutionRoot(string zipFileName)
    {
        // Walk up from current directory to find solution root
        var current = AppContext.BaseDirectory;
        while (current != null)
        {
            var datasetsPath = Path.Combine(current, "datasets", "test-fixtures", zipFileName);
            if (File.Exists(datasetsPath))
            {
                return datasetsPath;
            }

            // Check if we're at solution root (has .sln files)
            if (Directory.GetFiles(current, "*.sln").Length > 0)
            {
                var path = Path.Combine(current, "datasets", "test-fixtures", zipFileName);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            current = Path.GetDirectoryName(current);
        }

        return null;
    }

    private static string? FindSolutionFile(string projectRoot, string? specificSolution)
    {
        if (specificSolution != null)
        {
            var specificPath = Path.Combine(projectRoot, specificSolution);
            if (File.Exists(specificPath))
            {
                return specificPath;
            }

            // Search recursively
            var found = Directory.EnumerateFiles(projectRoot, specificSolution, SearchOption.AllDirectories)
                .FirstOrDefault();
            if (found != null)
            {
                return found;
            }
        }

        // Find any solution file
        var solutions = Directory.EnumerateFiles(projectRoot, "*.sln", SearchOption.AllDirectories).ToList();
        
        // Prefer solution in root
        var rootSolution = solutions.FirstOrDefault(s => 
            Path.GetDirectoryName(s) == projectRoot);
        
        return rootSolution ?? solutions.FirstOrDefault();
    }

    private static string? FindProjectFile(string projectRoot, string projectFileName)
    {
        var specificPath = Path.Combine(projectRoot, projectFileName);
        if (File.Exists(specificPath))
        {
            return specificPath;
        }

        // Search recursively
        return Directory.EnumerateFiles(projectRoot, projectFileName, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private static string? FindMainProjectFile(string projectRoot)
    {
        // Find all project files
        var projects = Directory.EnumerateFiles(projectRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains(".Tests") && !p.Contains("Benchmark"))
            .ToList();
        
        // Prefer project in src folder
        var srcProject = projects.FirstOrDefault(p => p.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}"));
        
        return srcProject ?? projects.FirstOrDefault();
    }

    /// <summary>
    /// Dispose does NOT delete the extraction directory - it stays for debugging.
    /// Directories are cleaned on next test run.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Intentionally NOT deleting _extractDir
        // The directory will be cleaned on next test run
    }
}

/// <summary>
/// xUnit class fixture for sharing Humanizer project across tests in a class.
/// Use with IClassFixture&lt;HumanizerFixture&gt; in test class.
/// </summary>
public class HumanizerFixture : IDisposable
{
    public TestProjectFixture Fixture { get; }

    public HumanizerFixture()
    {
        // Use class name as test name for shared fixture
        Fixture = TestProjectFixture.FromZip(
            "Humanizer-slim.zip", 
            "Humanizer.csproj", 
            "HumanizerFixture_Shared");
    }

    public void Dispose()
    {
        Fixture.Dispose();
    }
}
