using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for updating solution (.sln) files.
/// </summary>
public class SolutionFileService : ISolutionFileService
{
    /// <inheritdoc />
    public async Task<SolutionUpdateResult> UpdateProjectPathAsync(
        string solutionPath, 
        string oldPath, 
        string newPath,
        CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(solutionPath, cancellationToken);
        
        // Normalize paths for comparison
        var normalizedOld = oldPath.Replace("/", "\\");
        var normalizedNew = newPath.Replace("/", "\\");
        
        var updated = content.Replace(normalizedOld, normalizedNew);

        if (content != updated)
        {
            await File.WriteAllTextAsync(solutionPath, updated, cancellationToken);
            return new SolutionUpdateResult(true, solutionPath, true);
        }

        return new SolutionUpdateResult(true, solutionPath, false, "Path not found in solution");
    }
}
