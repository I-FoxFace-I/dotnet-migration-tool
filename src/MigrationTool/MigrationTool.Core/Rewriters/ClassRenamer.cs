using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrationTool.Core.Rewriters;

/// <summary>
/// Roslyn syntax rewriter for renaming classes, interfaces, records, and structs.
/// Also updates constructors and references within the same file.
/// </summary>
public class ClassRenamer : CSharpSyntaxRewriter
{
    private readonly string _oldName;
    private readonly string _newName;

    /// <summary>
    /// Gets the number of changes made during the rewrite operation.
    /// </summary>
    public int ChangesCount { get; private set; }

    /// <summary>
    /// Gets whether the main type declaration was renamed.
    /// </summary>
    public bool TypeRenamed { get; private set; }

    /// <summary>
    /// Gets whether constructors were renamed.
    /// </summary>
    public bool ConstructorsRenamed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassRenamer"/> class.
    /// </summary>
    /// <param name="oldName">The old type name to replace.</param>
    /// <param name="newName">The new type name to use.</param>
    public ClassRenamer(string oldName, string newName)
    {
        _oldName = oldName;
        _newName = newName;
    }

    /// <summary>
    /// Visits class declarations and renames matching classes.
    /// </summary>
    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            ChangesCount++;
            TypeRenamed = true;
            node = node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                .WithTriviaFrom(node.Identifier));
        }
        return base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Visits interface declarations and renames matching interfaces.
    /// </summary>
    public override SyntaxNode? VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            ChangesCount++;
            TypeRenamed = true;
            node = node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                .WithTriviaFrom(node.Identifier));
        }
        return base.VisitInterfaceDeclaration(node);
    }

    /// <summary>
    /// Visits record declarations and renames matching records.
    /// </summary>
    public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            ChangesCount++;
            TypeRenamed = true;
            node = node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                .WithTriviaFrom(node.Identifier));
        }
        return base.VisitRecordDeclaration(node);
    }

    /// <summary>
    /// Visits struct declarations and renames matching structs.
    /// </summary>
    public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            ChangesCount++;
            TypeRenamed = true;
            node = node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                .WithTriviaFrom(node.Identifier));
        }
        return base.VisitStructDeclaration(node);
    }

    /// <summary>
    /// Visits constructor declarations and renames matching constructors.
    /// </summary>
    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            ChangesCount++;
            ConstructorsRenamed = true;
            node = node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                .WithTriviaFrom(node.Identifier));
        }
        return base.VisitConstructorDeclaration(node);
    }

    /// <summary>
    /// Visits destructor declarations and renames matching destructors.
    /// </summary>
    public override SyntaxNode? VisitDestructorDeclaration(DestructorDeclarationSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            ChangesCount++;
            node = node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                .WithTriviaFrom(node.Identifier));
        }
        return base.VisitDestructorDeclaration(node);
    }

    /// <summary>
    /// Visits identifier names and renames references to the old type.
    /// This handles type references like variable declarations, method parameters, etc.
    /// </summary>
    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            // Check if this is actually a type reference (not a variable or method name)
            var parent = node.Parent;
            if (IsTypeContext(parent))
            {
                ChangesCount++;
                return node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                    .WithTriviaFrom(node.Identifier));
            }
        }
        return base.VisitIdentifierName(node);
    }

    /// <summary>
    /// Visits generic names and renames matching generic type references.
    /// </summary>
    public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    {
        if (node.Identifier.Text == _oldName)
        {
            var parent = node.Parent;
            if (IsTypeContext(parent))
            {
                ChangesCount++;
                return node.WithIdentifier(SyntaxFactory.Identifier(_newName)
                    .WithTriviaFrom(node.Identifier));
            }
        }
        return base.VisitGenericName(node);
    }

    private static bool IsTypeContext(SyntaxNode? parent)
    {
        return parent is
            TypeSyntax or
            BaseTypeSyntax or
            TypeConstraintSyntax or
            ObjectCreationExpressionSyntax or
            TypeOfExpressionSyntax or
            CastExpressionSyntax or
            IsPatternExpressionSyntax or
            DeclarationPatternSyntax or
            TypeParameterConstraintClauseSyntax or
            AttributeSyntax or
            QualifiedNameSyntax;
    }
}
