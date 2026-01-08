namespace MigrationTool.Core.Utilities;

/// <summary>
/// Utility class for detecting namespaces from file system paths.
/// </summary>
public static class NamespaceDetector
{
    /// <summary>
    /// Detects the namespace from a folder path by finding the nearest .csproj file
    /// and constructing the namespace from the project name and relative path.
    /// </summary>
    /// <param name="path">The folder path to analyze.</param>
    /// <returns>The detected namespace, or null if no project file was found.</returns>
    /// <example>
    /// C:\repo\src\MyProject\Services -> MyProject.Services
    /// C:\repo\src\MyProject\Data\Repositories -> MyProject.Data.Repositories
    /// </example>
    public static string? DetectFromPath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var current = new DirectoryInfo(fullPath);
        
        // If the directory doesn't exist, go up until we find one that does
        while (current != null && !current.Exists)
        {
            current = current.Parent;
        }
        
        // Traverse up from the existing directory to find .csproj
        while (current != null)
        {
            if (current.Exists && current.GetFiles("*.csproj").Any())
            {
                var projectName = current.Name;
                var relativePath = Path.GetRelativePath(current.FullName, fullPath);
                
                if (string.IsNullOrEmpty(relativePath) || relativePath == ".")
                    return projectName;
                    
                var subPath = relativePath
                    .Replace(Path.DirectorySeparatorChar, '.')
                    .Replace(Path.AltDirectorySeparatorChar, '.');
                return $"{projectName}.{subPath}";
            }
            current = current.Parent;
        }
        
        return null;
    }
}
