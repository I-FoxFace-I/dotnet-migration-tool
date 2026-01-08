using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;

using TypeInfo = MigrationTool.Core.Abstractions.Models.TypeInfo;
using TypeKind = MigrationTool.Core.Abstractions.Models.TypeKind;

namespace MigrationTool.Core.Analyzers;

/// <summary>
/// Analyzes C# source code using Roslyn.
/// </summary>
public class CodeAnalyzer : ICodeAnalyzer
{
    private readonly IFileSystemService _fileSystem;
    private readonly ILogger<CodeAnalyzer> _logger;

    // Test attributes
    private static readonly HashSet<string> TestAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Fact", "Theory", "Test", "TestMethod", "TestCase",
        "InlineData", "MemberData", "ClassData"
    };

    private static readonly HashSet<string> XUnitAttributes = new(StringComparer.OrdinalIgnoreCase) { "Fact", "Theory" };
    private static readonly HashSet<string> NUnitAttributes = new(StringComparer.OrdinalIgnoreCase) { "Test", "TestCase" };
    private static readonly HashSet<string> MSTestAttributes = new(StringComparer.OrdinalIgnoreCase) { "TestMethod" };

    public CodeAnalyzer(IFileSystemService fileSystem, ILogger<CodeAnalyzer> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SourceFileInfo> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var content = await _fileSystem.ReadFileAsync(filePath, cancellationToken);
        var result = await AnalyzeContentAsync(content, Path.GetFileName(filePath), cancellationToken);

        return result with
        {
            Path = filePath,
            RelativePath = null // Will be set by caller if needed
        };
    }

    /// <inheritdoc />
    public Task<SourceFileInfo> AnalyzeContentAsync(string content, string fileName = "temp.cs", CancellationToken cancellationToken = default)
    {
        var tree = CSharpSyntaxTree.ParseText(content, cancellationToken: cancellationToken);
        var root = tree.GetCompilationUnitRoot(cancellationToken);

        // Extract usings
        var usings = root.Usings
            .Select(u => u.Name?.ToString() ?? string.Empty)
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList();

        // Extract namespace
        var namespaceDecl = root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        var @namespace = namespaceDecl?.Name.ToString();

        // Extract types
        var types = ExtractTypes(root, @namespace);

        // Determine if test file
        var isTestFile = types.Any(t => t.Tests.Count > 0);

        var lineCount = content.Split('\n').Length;

        return Task.FromResult(new SourceFileInfo
        {
            Name = fileName,
            Path = fileName,
            Namespace = @namespace,
            Usings = usings,
            Classes = types,
            LineCount = lineCount,
            IsTestFile = isTestFile
        });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SourceFileInfo>> ScanDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scanning directory: {DirectoryPath}", directoryPath);

        var files = await _fileSystem.GetFilesAsync(directoryPath, "*.cs", recursive, cancellationToken);
        var results = new List<SourceFileInfo>();

        foreach (var file in files)
        {
            // Skip generated files
            if (file.Contains("obj", StringComparison.OrdinalIgnoreCase) ||
                file.Contains("bin", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var info = await AnalyzeFileAsync(file, cancellationToken);
                var relativePath = Path.GetRelativePath(directoryPath, file);
                results.Add(info with { RelativePath = relativePath });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze file: {FilePath}", file);
            }
        }

        _logger.LogInformation("Scanned {Count} files in {DirectoryPath}", results.Count, directoryPath);

        return results;
    }

    private List<TypeInfo> ExtractTypes(CompilationUnitSyntax root, string? defaultNamespace)
    {
        var types = new List<TypeInfo>();

        foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var typeInfo = ExtractTypeInfo(typeDecl, defaultNamespace);
            types.Add(typeInfo);
        }

        foreach (var enumDecl in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            types.Add(ExtractEnumInfo(enumDecl, defaultNamespace));
        }

        return types;
    }

    private TypeInfo ExtractTypeInfo(TypeDeclarationSyntax typeDecl, string? defaultNamespace)
    {
        var name = typeDecl.Identifier.Text;
        var kind = GetTypeKind(typeDecl);
        var accessModifier = GetAccessModifier(typeDecl.Modifiers);

        // Get namespace from parent or use default
        var typeNamespace = typeDecl.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault()?.Name.ToString() ?? defaultNamespace;

        // Get base types
        var baseTypes = typeDecl.BaseList?.Types
            .Select(t => t.Type.ToString())
            .ToList() ?? [];

        // Get methods
        var methods = typeDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(m => new MethodInfo(
                m.Identifier.Text,
                m.ReturnType.ToString(),
                m.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToList(),
                GetAccessModifier(m.Modifiers),
                m.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            ))
            .ToList();

        // Get properties
        var properties = typeDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => new PropertyInfo(
                p.Identifier.Text,
                p.Type.ToString(),
                GetAccessModifier(p.Modifiers),
                p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? p.ExpressionBody != null,
                p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
                p.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            ))
            .ToList();

        // Get tests
        var tests = ExtractTests(typeDecl, name);

        return new TypeInfo
        {
            Name = name,
            FullName = string.IsNullOrEmpty(typeNamespace) ? name : $"{typeNamespace}.{name}",
            Namespace = typeNamespace,
            Kind = kind,
            AccessModifier = accessModifier,
            BaseTypes = baseTypes,
            Methods = methods,
            Properties = properties,
            Tests = tests,
            LineNumber = typeDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };
    }

    private TypeInfo ExtractEnumInfo(EnumDeclarationSyntax enumDecl, string? defaultNamespace)
    {
        var name = enumDecl.Identifier.Text;
        var typeNamespace = enumDecl.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault()?.Name.ToString() ?? defaultNamespace;

        return new TypeInfo
        {
            Name = name,
            FullName = string.IsNullOrEmpty(typeNamespace) ? name : $"{typeNamespace}.{name}",
            Namespace = typeNamespace,
            Kind = TypeKind.Enum,
            AccessModifier = GetAccessModifier(enumDecl.Modifiers),
            LineNumber = enumDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1
        };
    }

    private List<TestInfo> ExtractTests(TypeDeclarationSyntax typeDecl, string className)
    {
        var tests = new List<TestInfo>();

        foreach (var method in typeDecl.Members.OfType<MethodDeclarationSyntax>())
        {
            var attributes = method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => a.Name.ToString())
                .ToList();

            var testAttribute = attributes.FirstOrDefault(a => TestAttributes.Contains(a));
            if (testAttribute == null) continue;

            var framework = DetermineTestFramework(testAttribute);

            tests.Add(new TestInfo
            {
                Name = method.Identifier.Text,
                ClassName = className,
                Framework = framework,
                Attributes = attributes,
                LineNumber = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            });
        }

        return tests;
    }

    private static TypeKind GetTypeKind(TypeDeclarationSyntax typeDecl)
    {
        return typeDecl switch
        {
            ClassDeclarationSyntax c when c.Modifiers.Any(SyntaxKind.RecordKeyword) => TypeKind.Record,
            ClassDeclarationSyntax => TypeKind.Class,
            InterfaceDeclarationSyntax => TypeKind.Interface,
            StructDeclarationSyntax s when s.Modifiers.Any(SyntaxKind.RecordKeyword) => TypeKind.RecordStruct,
            StructDeclarationSyntax => TypeKind.Struct,
            RecordDeclarationSyntax r when r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => TypeKind.RecordStruct,
            RecordDeclarationSyntax => TypeKind.Record,
            _ => TypeKind.Class
        };
    }

    private static AccessModifier GetAccessModifier(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PublicKeyword))
            return AccessModifier.Public;
        if (modifiers.Any(SyntaxKind.PrivateKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
            return AccessModifier.PrivateProtected;
        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && modifiers.Any(SyntaxKind.InternalKeyword))
            return AccessModifier.ProtectedInternal;
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            return AccessModifier.Protected;
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return AccessModifier.Private;
        if (modifiers.Any(SyntaxKind.InternalKeyword))
            return AccessModifier.Internal;

        return AccessModifier.Internal; // Default for C#
    }

    private static TestFramework DetermineTestFramework(string attribute)
    {
        if (XUnitAttributes.Contains(attribute))
            return TestFramework.XUnit;
        if (NUnitAttributes.Contains(attribute))
            return TestFramework.NUnit;
        if (MSTestAttributes.Contains(attribute))
            return TestFramework.MSTest;

        return TestFramework.Unknown;
    }
}
