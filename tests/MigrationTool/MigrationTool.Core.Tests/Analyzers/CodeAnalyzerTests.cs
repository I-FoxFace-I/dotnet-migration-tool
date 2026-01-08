using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MigrationTool.Core.Abstractions.Models;
using MigrationTool.Core.Abstractions.Services;
using MigrationTool.Core.Analyzers;
using Moq;
using Xunit;

namespace MigrationTool.Core.Tests.Analyzers;

public class CodeAnalyzerTests
{
    private readonly Mock<IFileSystemService> _fileSystemMock;
    private readonly CodeAnalyzer _analyzer;

    public CodeAnalyzerTests()
    {
        _fileSystemMock = new Mock<IFileSystemService>();
        _analyzer = new CodeAnalyzer(_fileSystemMock.Object, NullLogger<CodeAnalyzer>.Instance);
    }

    [Fact]
    public async Task AnalyzeContentAsync_SimpleClass_ExtractsCorrectInfo()
    {
        // Arrange
        var content = """
            namespace MyApp.Services;

            public class MyService
            {
                public void DoSomething() { }
                public string Name { get; set; }
            }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "MyService.cs");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MyService.cs");
        result.Namespace.Should().Be("MyApp.Services");
        result.Classes.Should().HaveCount(1);
        result.Classes[0].Name.Should().Be("MyService");
        result.Classes[0].Kind.Should().Be(TypeKind.Class);
        result.Classes[0].AccessModifier.Should().Be(AccessModifier.Public);
        result.Classes[0].Methods.Should().HaveCount(1);
        result.Classes[0].Properties.Should().HaveCount(1);
    }

    [Fact]
    public async Task AnalyzeContentAsync_Interface_ExtractsCorrectKind()
    {
        // Arrange
        var content = """
            namespace MyApp.Abstractions;

            public interface IMyService
            {
                void DoSomething();
            }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "IMyService.cs");

        // Assert
        result.Classes.Should().HaveCount(1);
        result.Classes[0].Name.Should().Be("IMyService");
        result.Classes[0].Kind.Should().Be(TypeKind.Interface);
    }

    [Fact]
    public async Task AnalyzeContentAsync_Record_ExtractsCorrectKind()
    {
        // Arrange
        var content = """
            namespace MyApp.Models;

            public record Person(string Name, int Age);
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "Person.cs");

        // Assert
        result.Classes.Should().HaveCount(1);
        result.Classes[0].Name.Should().Be("Person");
        result.Classes[0].Kind.Should().Be(TypeKind.Record);
    }

    [Fact]
    public async Task AnalyzeContentAsync_Enum_ExtractsCorrectKind()
    {
        // Arrange
        var content = """
            namespace MyApp.Models;

            public enum Status
            {
                Active,
                Inactive,
                Pending
            }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "Status.cs");

        // Assert
        result.Classes.Should().HaveCount(1);
        result.Classes[0].Name.Should().Be("Status");
        result.Classes[0].Kind.Should().Be(TypeKind.Enum);
    }

    [Fact]
    public async Task AnalyzeContentAsync_XUnitTests_ExtractsTests()
    {
        // Arrange
        var content = """
            using Xunit;

            namespace MyApp.Tests;

            public class MyServiceTests
            {
                [Fact]
                public void DoSomething_ShouldWork()
                {
                }

                [Theory]
                [InlineData(1)]
                [InlineData(2)]
                public void DoSomething_WithData_ShouldWork(int value)
                {
                }
            }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "MyServiceTests.cs");

        // Assert
        result.IsTestFile.Should().BeTrue();
        result.TestCount.Should().Be(2);
        result.Classes[0].Tests.Should().HaveCount(2);
        result.Classes[0].Tests[0].Name.Should().Be("DoSomething_ShouldWork");
        result.Classes[0].Tests[0].Framework.Should().Be(TestFramework.XUnit);
        result.Classes[0].Tests[1].Name.Should().Be("DoSomething_WithData_ShouldWork");
    }

    [Fact]
    public async Task AnalyzeContentAsync_MultipleClasses_ExtractsAll()
    {
        // Arrange
        var content = """
            namespace MyApp.Models;

            public class Person
            {
                public string Name { get; set; }
            }

            public class Address
            {
                public string Street { get; set; }
            }

            internal class Helper
            {
            }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "Models.cs");

        // Assert
        result.Classes.Should().HaveCount(3);
        result.Classes.Select(c => c.Name).Should().Contain(["Person", "Address", "Helper"]);
        result.Classes.Single(c => c.Name == "Helper").AccessModifier.Should().Be(AccessModifier.Internal);
    }

    [Fact]
    public async Task AnalyzeContentAsync_WithUsings_ExtractsUsings()
    {
        // Arrange
        var content = """
            using System;
            using System.Collections.Generic;
            using MyApp.Services;

            namespace MyApp.Models;

            public class MyClass { }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "MyClass.cs");

        // Assert
        result.Usings.Should().HaveCount(3);
        result.Usings.Should().Contain(["System", "System.Collections.Generic", "MyApp.Services"]);
    }

    [Fact]
    public async Task AnalyzeContentAsync_InheritedClass_ExtractsBaseTypes()
    {
        // Arrange
        var content = """
            namespace MyApp.Services;

            public class MyService : BaseService, IMyService, IDisposable
            {
            }
            """;

        // Act
        var result = await _analyzer.AnalyzeContentAsync(content, "MyService.cs");

        // Assert
        result.Classes[0].BaseTypes.Should().HaveCount(3);
        result.Classes[0].BaseTypes.Should().Contain(["BaseService", "IMyService", "IDisposable"]);
    }

    [Fact]
    public async Task AnalyzeFileAsync_ReadsFileAndAnalyzes()
    {
        // Arrange
        var filePath = "/test/MyClass.cs";
        var content = """
            namespace Test;
            public class MyClass { }
            """;

        _fileSystemMock.Setup(x => x.ReadFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);

        // Act
        var result = await _analyzer.AnalyzeFileAsync(filePath);

        // Assert
        result.Path.Should().Be(filePath);
        result.Classes.Should().HaveCount(1);
    }

    [Fact]
    public async Task ScanDirectoryAsync_ScansAllCsFiles()
    {
        // Arrange
        var dirPath = "/test/src";
        var files = new[] { "/test/src/Class1.cs", "/test/src/Class2.cs" };

        _fileSystemMock.Setup(x => x.GetFilesAsync(dirPath, "*.cs", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        _fileSystemMock.Setup(x => x.ReadFileAsync("/test/src/Class1.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("namespace Test; public class Class1 { }");

        _fileSystemMock.Setup(x => x.ReadFileAsync("/test/src/Class2.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("namespace Test; public class Class2 { }");

        // Act
        var results = (await _analyzer.ScanDirectoryAsync(dirPath)).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.Classes[0].Name).Should().Contain(["Class1", "Class2"]);
    }

    [Fact]
    public async Task ScanDirectoryAsync_SkipsGeneratedFiles()
    {
        // Arrange
        var dirPath = "/test/src";
        var files = new[]
        {
            "/test/src/Class1.cs",
            "/test/src/obj/Debug/Generated.cs",
            "/test/src/bin/Release/Output.cs",
            "/test/src/Class1.g.cs",
            "/test/src/Form.Designer.cs"
        };

        _fileSystemMock.Setup(x => x.GetFilesAsync(dirPath, "*.cs", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        _fileSystemMock.Setup(x => x.ReadFileAsync("/test/src/Class1.cs", It.IsAny<CancellationToken>()))
            .ReturnsAsync("namespace Test; public class Class1 { }");

        // Act
        var results = (await _analyzer.ScanDirectoryAsync(dirPath)).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Classes[0].Name.Should().Be("Class1");
    }
}
