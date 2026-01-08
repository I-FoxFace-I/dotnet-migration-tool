using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using MigrationTool.Core.Abstractions.Services;

namespace MigrationTool.Core.Services;

/// <summary>
/// Service for finding symbol usages in a solution using Roslyn.
/// </summary>
public class SymbolUsageService : ISymbolUsageService
{
    /// <inheritdoc />
    public async Task<SymbolUsageResult> FindUsagesAsync(
        string solutionPath, 
        string symbol,
        CancellationToken cancellationToken = default)
    {
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        var usages = new List<SymbolUsage>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var root = await document.GetSyntaxRootAsync(cancellationToken);
                if (root == null) continue;

                var identifiers = root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(i => i.Identifier.Text == symbol);

                foreach (var id in identifiers)
                {
                    var lineSpan = id.GetLocation().GetLineSpan();
                    usages.Add(new SymbolUsage(
                        document.FilePath ?? string.Empty,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1));
                }
            }
        }

        return new SymbolUsageResult(true, symbol, usages.Count, usages);
    }
}
