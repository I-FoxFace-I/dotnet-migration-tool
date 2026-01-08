using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using MigrationTool.Core.Rewriters;
using Xunit;

namespace MigrationTool.Cli.Tests;

/// <summary>
/// Tests for the NamespaceRewriter class.
/// </summary>
public class NamespaceRewriterTests
{
    [Fact]
    public void Visit_FileScopedNamespace_UpdatesNamespace()
    {
        // Arrange
        var code = """
            namespace OldNamespace;

            public class MyClass { }
            """;
        
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new NamespaceRewriter("OldNamespace", "NewNamespace");
        
        // Act
        var newRoot = rewriter.Visit(root);
        var result = newRoot.ToFullString();
        
        // Assert
        result.Should().Contain("namespace NewNamespace;");
        result.Should().NotContain("namespace OldNamespace;");
        rewriter.ChangesCount.Should().Be(1);
    }
    
    [Fact]
    public void Visit_BlockScopedNamespace_UpdatesNamespace()
    {
        // Arrange
        var code = """
            namespace OldNamespace
            {
                public class MyClass { }
            }
            """;
        
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new NamespaceRewriter("OldNamespace", "NewNamespace");
        
        // Act
        var newRoot = rewriter.Visit(root);
        var result = newRoot.ToFullString();
        
        // Assert
        result.Should().Contain("namespace NewNamespace");
        result.Should().NotContain("namespace OldNamespace");
        rewriter.ChangesCount.Should().Be(1);
    }
    
    [Fact]
    public void Visit_UsingDirective_UpdatesUsing()
    {
        // Arrange
        var code = """
            using OldNamespace;
            using OldNamespace.SubNamespace;
            using System;

            namespace Test;

            public class MyClass { }
            """;
        
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new NamespaceRewriter("OldNamespace", "NewNamespace");
        
        // Act
        var newRoot = rewriter.Visit(root);
        var result = newRoot.ToFullString();
        
        // Assert
        result.Should().Contain("using NewNamespace;");
        result.Should().Contain("using NewNamespace.SubNamespace;");
        result.Should().Contain("using System;");
        result.Should().NotContain("using OldNamespace;");
        rewriter.ChangesCount.Should().Be(2);
    }
    
    [Fact]
    public void Visit_NoMatchingNamespace_NoChanges()
    {
        // Arrange
        var code = """
            namespace DifferentNamespace;

            public class MyClass { }
            """;
        
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new NamespaceRewriter("OldNamespace", "NewNamespace");
        
        // Act
        var newRoot = rewriter.Visit(root);
        var result = newRoot.ToFullString();
        
        // Assert
        result.Should().Contain("namespace DifferentNamespace;");
        rewriter.ChangesCount.Should().Be(0);
    }
    
    [Fact]
    public void Visit_NestedNamespace_UpdatesCorrectly()
    {
        // Arrange
        var code = """
            namespace OldNamespace.Models;

            public class MyModel { }
            """;
        
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new NamespaceRewriter("OldNamespace.Models", "NewNamespace.Entities");
        
        // Act
        var newRoot = rewriter.Visit(root);
        var result = newRoot.ToFullString();
        
        // Assert
        result.Should().Contain("namespace NewNamespace.Entities;");
        result.Should().NotContain("OldNamespace");
        rewriter.ChangesCount.Should().Be(1);
    }
    
    [Fact]
    public void Visit_MultipleUsings_UpdatesAll()
    {
        // Arrange
        var code = """
            using Wpf.Scopes.Tests;
            using Wpf.Scopes.Tests.Core;
            using Wpf.Scopes.Tests.Helpers;
            using System.Linq;

            namespace TestProject;

            public class MyTests { }
            """;
        
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        var rewriter = new NamespaceRewriter("Wpf.Scopes.Tests", "Wpf.Scopes.Unit.Tests");
        
        // Act
        var newRoot = rewriter.Visit(root);
        var result = newRoot.ToFullString();
        
        // Assert
        result.Should().Contain("using Wpf.Scopes.Unit.Tests;");
        result.Should().Contain("using Wpf.Scopes.Unit.Tests.Core;");
        result.Should().Contain("using Wpf.Scopes.Unit.Tests.Helpers;");
        result.Should().Contain("using System.Linq;");
        rewriter.ChangesCount.Should().Be(3);
    }
}
