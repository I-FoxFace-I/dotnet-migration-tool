using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationTool.Core.Rewriters;

/// <summary>
/// Roslyn syntax rewriter for updating using directives.
/// Can add, remove, or replace using directives.
/// </summary>
public class UsingRewriter : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, string> _replacements;
    private readonly HashSet<string> _toRemove;
    private readonly HashSet<string> _toAdd;

    /// <summary>
    /// Gets the number of changes made during the rewrite operation.
    /// </summary>
    public int ChangesCount { get; private set; }

    /// <summary>
    /// Gets the list of added using directives.
    /// </summary>
    public IReadOnlyList<string> AddedUsings => _addedUsings;
    private readonly List<string> _addedUsings = [];

    /// <summary>
    /// Gets the list of removed using directives.
    /// </summary>
    public IReadOnlyList<string> RemovedUsings => _removedUsings;
    private readonly List<string> _removedUsings = [];

    /// <summary>
    /// Gets the list of replaced using directives.
    /// </summary>
    public IReadOnlyList<(string Old, string New)> ReplacedUsings => _replacedUsings;
    private readonly List<(string Old, string New)> _replacedUsings = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="UsingRewriter"/> class.
    /// </summary>
    public UsingRewriter()
    {
        _replacements = new Dictionary<string, string>();
        _toRemove = new HashSet<string>();
        _toAdd = new HashSet<string>();
    }

    /// <summary>
    /// Adds a namespace replacement rule.
    /// </summary>
    /// <param name="oldNamespace">The namespace to replace.</param>
    /// <param name="newNamespace">The new namespace to use.</param>
    /// <returns>This instance for method chaining.</returns>
    public UsingRewriter WithReplacement(string oldNamespace, string newNamespace)
    {
        _replacements[oldNamespace] = newNamespace;
        return this;
    }

    /// <summary>
    /// Adds multiple namespace replacement rules.
    /// </summary>
    /// <param name="replacements">Dictionary of old to new namespace mappings.</param>
    /// <returns>This instance for method chaining.</returns>
    public UsingRewriter WithReplacements(IDictionary<string, string> replacements)
    {
        foreach (var (old, @new) in replacements)
        {
            _replacements[old] = @new;
        }
        return this;
    }

    /// <summary>
    /// Marks a namespace to be removed from using directives.
    /// </summary>
    /// <param name="namespaceToRemove">The namespace to remove.</param>
    /// <returns>This instance for method chaining.</returns>
    public UsingRewriter WithRemoval(string namespaceToRemove)
    {
        _toRemove.Add(namespaceToRemove);
        return this;
    }

    /// <summary>
    /// Marks a namespace to be added to using directives.
    /// </summary>
    /// <param name="namespaceToAdd">The namespace to add.</param>
    /// <returns>This instance for method chaining.</returns>
    public UsingRewriter WithAddition(string namespaceToAdd)
    {
        _toAdd.Add(namespaceToAdd);
        return this;
    }

    /// <summary>
    /// Visits using directives and applies configured transformations.
    /// </summary>
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        var namespaceName = node.Name?.ToString();
        if (string.IsNullOrEmpty(namespaceName))
        {
            return base.VisitUsingDirective(node);
        }

        // Check for removal
        if (_toRemove.Contains(namespaceName) || _toRemove.Any(r => namespaceName.StartsWith(r + ".")))
        {
            ChangesCount++;
            _removedUsings.Add(namespaceName);
            return null; // Remove the using directive
        }

        // Check for replacement (exact match first)
        if (_replacements.TryGetValue(namespaceName, out var exactReplacement))
        {
            ChangesCount++;
            _replacedUsings.Add((namespaceName, exactReplacement));
            return node.WithName(SyntaxFactory.ParseName(exactReplacement));
        }

        // Check for prefix replacement
        foreach (var (oldPrefix, newPrefix) in _replacements)
        {
            if (namespaceName.StartsWith(oldPrefix + "."))
            {
                var newName = newPrefix + namespaceName.Substring(oldPrefix.Length);
                ChangesCount++;
                _replacedUsings.Add((namespaceName, newName));
                return node.WithName(SyntaxFactory.ParseName(newName));
            }
            
            if (namespaceName == oldPrefix)
            {
                ChangesCount++;
                _replacedUsings.Add((namespaceName, newPrefix));
                return node.WithName(SyntaxFactory.ParseName(newPrefix));
            }
        }

        return base.VisitUsingDirective(node);
    }

    /// <summary>
    /// Visits compilation unit and adds new using directives if configured.
    /// </summary>
    public override SyntaxNode? VisitCompilationUnit(CompilationUnitSyntax node)
    {
        var result = (CompilationUnitSyntax?)base.VisitCompilationUnit(node);
        
        if (result == null || !_toAdd.Any())
        {
            return result;
        }

        // Get existing usings
        var existingUsings = result.Usings
            .Select(u => u.Name?.ToString())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet();

        // Add new usings that don't already exist
        var newUsings = new List<UsingDirectiveSyntax>();
        foreach (var ns in _toAdd.OrderBy(n => n))
        {
            if (!existingUsings.Contains(ns))
            {
                var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns))
                    .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space))
                    .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
                newUsings.Add(usingDirective);
                ChangesCount++;
                _addedUsings.Add(ns);
            }
        }

        if (newUsings.Any())
        {
            result = result.AddUsings(newUsings.ToArray());
        }

        return result;
    }
}
