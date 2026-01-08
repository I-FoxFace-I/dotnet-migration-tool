using Microsoft.CodeAnalysis.CSharp;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Rewriters;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for updating namespaces in C# files using Roslyn.
/// </summary>
public class NamespaceService : INamespaceService
{
    /// <inheritdoc />
    public async Task<NamespaceUpdateResult> UpdateNamespaceAsync(
        string filePath, 
        string oldNamespace, 
        string newNamespace,
        CancellationToken cancellationToken = default)
    {
        var code = await File.ReadAllTextAsync(filePath, cancellationToken);
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
        var root = await tree.GetRootAsync(cancellationToken);

        var rewriter = new NamespaceRewriter(oldNamespace, newNamespace);
        var newRoot = rewriter.Visit(root);

        if (rewriter.ChangesCount > 0)
        {
            await File.WriteAllTextAsync(filePath, newRoot.ToFullString(), cancellationToken);
            return new NamespaceUpdateResult(true, filePath, rewriter.ChangesCount);
        }

        return new NamespaceUpdateResult(true, filePath, 0, "No changes needed");
    }
}
