using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for updating project references in .csproj files.
/// </summary>
public class ProjectRefService : IProjectRefService
{
    /// <inheritdoc />
    public async Task<ProjectRefUpdateResult> UpdateReferenceAsync(
        string projectPath, 
        string oldRef, 
        string newRef,
        CancellationToken cancellationToken = default)
    {
        var content = await File.ReadAllTextAsync(projectPath, cancellationToken);
        var updated = content.Replace(oldRef, newRef);

        if (content != updated)
        {
            await File.WriteAllTextAsync(projectPath, updated, cancellationToken);
            return new ProjectRefUpdateResult(true, projectPath, oldRef, newRef);
        }

        return new ProjectRefUpdateResult(true, projectPath, Message: "Reference not found");
    }
}
