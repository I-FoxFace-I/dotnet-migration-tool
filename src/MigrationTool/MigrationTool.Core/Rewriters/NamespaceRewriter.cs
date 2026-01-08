using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationTool.Core.Rewriters;

/// <summary>
/// Roslyn syntax rewriter for namespace updates.
/// Updates namespace declarations and using directives.
/// </summary>
public class NamespaceRewriter : CSharpSyntaxRewriter
{
    private readonly string _oldNamespace;
    private readonly string _newNamespace;

    /// <summary>
    /// Gets the number of changes made during the rewrite operation.
    /// </summary>
    public int ChangesCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceRewriter"/> class.
    /// </summary>
    /// <param name="oldNamespace">The namespace to replace.</param>
    /// <param name="newNamespace">The new namespace to use.</param>
    public NamespaceRewriter(string oldNamespace, string newNamespace)
    {
        _oldNamespace = oldNamespace;
        _newNamespace = newNamespace;
    }

    /// <summary>
    /// Visits file-scoped namespace declarations (e.g., namespace MyNamespace;).
    /// </summary>
    public override SyntaxNode? VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
    {
        if (node.Name.ToString() == _oldNamespace)
        {
            ChangesCount++;
            return node.WithName(SyntaxFactory.ParseName(_newNamespace));
        }
        return base.VisitFileScopedNamespaceDeclaration(node);
    }

    /// <summary>
    /// Visits block-scoped namespace declarations (e.g., namespace MyNamespace { }).
    /// </summary>
    public override SyntaxNode? VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
    {
        if (node.Name.ToString() == _oldNamespace)
        {
            ChangesCount++;
            return node.WithName(SyntaxFactory.ParseName(_newNamespace));
        }
        return base.VisitNamespaceDeclaration(node);
    }

    /// <summary>
    /// Visits using directives and updates those that start with the old namespace.
    /// </summary>
    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (node.Name?.ToString().StartsWith(_oldNamespace) == true)
        {
            var newName = node.Name.ToString().Replace(_oldNamespace, _newNamespace);
            ChangesCount++;
            return node.WithName(SyntaxFactory.ParseName(newName));
        }
        return base.VisitUsingDirective(node);
    }
}
