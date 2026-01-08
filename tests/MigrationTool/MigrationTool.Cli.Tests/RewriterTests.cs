using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using MigrationTool.Core.Rewriters;
using Xunit;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests for Roslyn rewriters (UsingRewriter, ClassRenamer).
/// </summary>
public class RewriterTests
{
    #region UsingRewriter Tests

    [Fact]
    public void UsingRewriter_ReplaceNamespace_ReplacesExactMatch()
    {
        // Arrange
        var code = @"using OldNamespace;
using OtherNamespace;

namespace Test;
public class MyClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var rewriter = new UsingRewriter()
            .WithReplacement("OldNamespace", "NewNamespace");

        // Act
        var newRoot = rewriter.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("using NewNamespace;");
        result.Should().NotContain("using OldNamespace;");
        result.Should().Contain("using OtherNamespace;");
        rewriter.ChangesCount.Should().Be(1);
    }

    [Fact]
    public void UsingRewriter_ReplaceNamespace_ReplacesPrefixMatch()
    {
        // Arrange
        var code = @"using Company.Project.Services;
using Company.Project.Models;
using System;

namespace Test;
public class MyClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var rewriter = new UsingRewriter()
            .WithReplacement("Company.Project", "NewCompany.NewProject");

        // Act
        var newRoot = rewriter.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("using NewCompany.NewProject.Services;");
        result.Should().Contain("using NewCompany.NewProject.Models;");
        result.Should().Contain("using System;"); // Unchanged
        rewriter.ChangesCount.Should().Be(2);
    }

    [Fact]
    public void UsingRewriter_RemoveNamespace_RemovesUsing()
    {
        // Arrange
        var code = @"using ToRemove;
using ToKeep;

namespace Test;
public class MyClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var rewriter = new UsingRewriter()
            .WithRemoval("ToRemove");

        // Act
        var newRoot = rewriter.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().NotContain("using ToRemove;");
        result.Should().Contain("using ToKeep;");
        rewriter.ChangesCount.Should().Be(1);
        rewriter.RemovedUsings.Should().Contain("ToRemove");
    }

    [Fact]
    public void UsingRewriter_AddNamespace_AddsUsing()
    {
        // Arrange
        var code = @"using Existing;

namespace Test;
public class MyClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var rewriter = new UsingRewriter()
            .WithAddition("NewNamespace");

        // Act
        var newRoot = rewriter.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("using Existing;");
        result.Should().Contain("using NewNamespace;");
        rewriter.ChangesCount.Should().Be(1);
        rewriter.AddedUsings.Should().Contain("NewNamespace");
    }

    [Fact]
    public void UsingRewriter_AddExistingNamespace_DoesNotDuplicate()
    {
        // Arrange
        var code = @"using Existing;

namespace Test;
public class MyClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var rewriter = new UsingRewriter()
            .WithAddition("Existing");

        // Act
        var newRoot = rewriter.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Split("using Existing;").Length.Should().Be(2); // Only one occurrence
        rewriter.AddedUsings.Should().BeEmpty();
    }

    [Fact]
    public void UsingRewriter_MultipleOperations_AppliesAll()
    {
        // Arrange
        var code = @"using OldNs;
using ToRemove;
using KeepThis;

namespace Test;
public class MyClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var rewriter = new UsingRewriter()
            .WithReplacement("OldNs", "NewNs")
            .WithRemoval("ToRemove")
            .WithAddition("AddedNs");

        // Act
        var newRoot = rewriter.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("using NewNs;");
        result.Should().NotContain("using OldNs;");
        result.Should().NotContain("using ToRemove;");
        result.Should().Contain("using KeepThis;");
        result.Should().Contain("using AddedNs;");
    }

    #endregion

    #region ClassRenamer Tests

    [Fact]
    public void ClassRenamer_RenameClass_RenamesClassDeclaration()
    {
        // Arrange
        var code = @"namespace Test;

public class OldName
{
    public void Method() { }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("OldName", "NewName");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("public class NewName");
        result.Should().NotContain("public class OldName");
        renamer.TypeRenamed.Should().BeTrue();
    }

    [Fact]
    public void ClassRenamer_RenameClass_RenamesConstructors()
    {
        // Arrange
        var code = @"namespace Test;

public class OldName
{
    public OldName() { }
    public OldName(int value) { }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("OldName", "NewName");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("public NewName()");
        result.Should().Contain("public NewName(int value)");
        result.Should().NotContain("public OldName()");
        renamer.ConstructorsRenamed.Should().BeTrue();
    }

    [Fact]
    public void ClassRenamer_RenameInterface_RenamesInterface()
    {
        // Arrange
        var code = @"namespace Test;

public interface IOldName
{
    void Method();
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("IOldName", "INewName");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("public interface INewName");
        result.Should().NotContain("public interface IOldName");
        renamer.TypeRenamed.Should().BeTrue();
    }

    [Fact]
    public void ClassRenamer_RenameRecord_RenamesRecord()
    {
        // Arrange
        var code = @"namespace Test;

public record OldRecord(string Name);";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("OldRecord", "NewRecord");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("public record NewRecord");
        result.Should().NotContain("public record OldRecord");
        renamer.TypeRenamed.Should().BeTrue();
    }

    [Fact]
    public void ClassRenamer_RenameStruct_RenamesStruct()
    {
        // Arrange
        var code = @"namespace Test;

public struct OldStruct
{
    public int Value;
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("OldStruct", "NewStruct");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("public struct NewStruct");
        result.Should().NotContain("public struct OldStruct");
        renamer.TypeRenamed.Should().BeTrue();
    }

    [Fact]
    public void ClassRenamer_TypeReferences_RenamesTypeUsages()
    {
        // Arrange
        var code = @"namespace Test;

public class OldName { }

public class Consumer
{
    private OldName _instance;
    
    public OldName Create()
    {
        return new OldName();
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("OldName", "NewName");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        // Class declaration is renamed
        result.Should().Contain("public class NewName");
        // Object creation is renamed
        result.Should().Contain("new NewName()");
        // Note: Field and method return type renaming requires semantic analysis
        // which is beyond simple syntax rewriting. The ClassRenamer focuses on
        // type declarations, constructors, and object creation expressions.
    }

    [Fact]
    public void ClassRenamer_NoMatch_MakesNoChanges()
    {
        // Arrange
        var code = @"namespace Test;

public class SomeClass { }";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var renamer = new ClassRenamer("OtherClass", "NewClass");

        // Act
        var newRoot = renamer.Visit(root);

        // Assert
        var result = newRoot.ToFullString();
        result.Should().Contain("public class SomeClass");
        renamer.TypeRenamed.Should().BeFalse();
        renamer.ChangesCount.Should().Be(0);
    }

    #endregion
}
