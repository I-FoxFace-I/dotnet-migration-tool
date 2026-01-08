using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MigrationTool.Maui.Tests.Services;

/// <summary>
/// Tests for ViewModelBindingValidator - validates ViewModel bindings.
/// </summary>
public class ViewModelBindingValidatorTests
{
    private readonly Mock<ILogger<TestableViewModelBindingValidator>> _loggerMock;

    public ViewModelBindingValidatorTests()
    {
        _loggerMock = new Mock<ILogger<TestableViewModelBindingValidator>>();
    }

    private TestableViewModelBindingValidator CreateValidator()
    {
        return new TestableViewModelBindingValidator(_loggerMock.Object);
    }

    [Fact]
    public void ValidateViewModel_WithAllPropertiesPresent_ReportsSuccess()
    {
        // Arrange
        var validator = CreateValidator();
        var expectedProperties = new[] { "Name", "Value" };
        var expectedCommands = Array.Empty<string>();

        // Act
        validator.ValidateViewModel<SampleViewModelWithProperties>(expectedProperties, expectedCommands);
        var report = validator.GetReport();

        // Assert
        report.IsValid.Should().BeTrue();
        report.FailedCount.Should().Be(0);
    }

    [Fact]
    public void ValidateViewModel_WithMissingProperty_ReportsFailure()
    {
        // Arrange
        var validator = CreateValidator();
        var expectedProperties = new[] { "Name", "MissingProperty" };
        var expectedCommands = Array.Empty<string>();

        // Act
        validator.ValidateViewModel<SampleViewModelWithProperties>(expectedProperties, expectedCommands);
        var report = validator.GetReport();

        // Assert
        report.HasErrors.Should().BeTrue();
        report.FailedResults.Should().Contain(r => r.Message.Contains("MissingProperty"));
    }

    [Fact]
    public void ValidateViewModel_WithAllCommandsPresent_ReportsSuccess()
    {
        // Arrange
        var validator = CreateValidator();
        var expectedProperties = new[] { "Name" };
        var expectedCommands = new[] { "SaveCommand", "LoadCommand" };

        // Act
        validator.ValidateViewModel<SampleViewModelWithCommands>(expectedProperties, expectedCommands);
        var report = validator.GetReport();

        // Assert
        report.IsValid.Should().BeTrue();
        report.FailedResults.Should().NotContain(r => r.Category == "Missing Commands");
    }

    [Fact]
    public void ValidateViewModel_WithMissingCommand_ReportsFailure()
    {
        // Arrange
        var validator = CreateValidator();
        var expectedProperties = new[] { "Name" };
        var expectedCommands = new[] { "SaveCommand", "DeleteCommand" };

        // Act
        validator.ValidateViewModel<SampleViewModelWithCommands>(expectedProperties, expectedCommands);
        var report = validator.GetReport();

        // Assert
        report.HasErrors.Should().BeTrue();
        report.FailedResults.Should().Contain(r => r.Message.Contains("DeleteCommand"));
    }

    [Fact]
    public void ValidateViewModel_WithExtraProperties_ReportsWarning()
    {
        // Arrange
        var validator = CreateValidator();
        var expectedProperties = new[] { "Name" };
        var expectedCommands = Array.Empty<string>();

        // Act
        validator.ValidateViewModel<SampleViewModelWithProperties>(expectedProperties, expectedCommands);
        var report = validator.GetReport();

        // Assert
        report.WarningResults.Should().Contain(r => r.Category == "Extra Properties");
    }

    [Fact]
    public void ValidateViewModel_WithExtraCommands_ReportsInfo()
    {
        // Arrange
        var validator = CreateValidator();
        var expectedProperties = new[] { "Name" };
        var expectedCommands = new[] { "SaveCommand" };

        // Act
        validator.ValidateViewModel<SampleViewModelWithCommands>(expectedProperties, expectedCommands);
        var report = validator.GetReport();

        // Assert
        report.InfoResults.Should().Contain(r => r.Category == "Extra Commands");
    }

    [Fact]
    public void Clear_RemovesAllResults()
    {
        // Arrange
        var validator = CreateValidator();
        validator.ValidateViewModel<SampleViewModelWithProperties>(new[] { "Name" }, Array.Empty<string>());
        validator.GetReport().TotalCount.Should().BeGreaterThan(0);

        // Act
        validator.Clear();

        // Assert
        validator.GetReport().TotalCount.Should().Be(0);
    }

    [Fact]
    public void GetReport_ReturnsCorrectCounts()
    {
        // Arrange
        var validator = CreateValidator();
        validator.ValidateViewModel<SampleViewModelWithProperties>(
            new[] { "Name", "MissingProp" }, // 1 missing = 1 failed
            Array.Empty<string>());

        // Act
        var report = validator.GetReport();

        // Assert
        report.TotalCount.Should().BeGreaterThan(0);
        report.FailedCount.Should().Be(1);
        report.PassedCount.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void ValidationReport_IsValid_WhenNoFailures()
    {
        // Arrange
        var validator = CreateValidator();
        validator.ValidateViewModel<SampleViewModelWithProperties>(
            new[] { "Name", "Value" },
            Array.Empty<string>());

        // Act
        var report = validator.GetReport();

        // Assert
        report.IsValid.Should().BeTrue();
        report.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void ValidationReport_HasErrors_WhenFailuresExist()
    {
        // Arrange
        var validator = CreateValidator();
        validator.ValidateViewModel<SampleViewModelWithProperties>(
            new[] { "NonExistent" },
            Array.Empty<string>());

        // Act
        var report = validator.GetReport();

        // Assert
        report.IsValid.Should().BeFalse();
        report.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void ValidateViewModel_LogsValidationStart()
    {
        // Arrange
        var validator = CreateValidator();

        // Act
        validator.ValidateViewModel<SampleViewModelWithProperties>(new[] { "Name" }, Array.Empty<string>());

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

#region Test ViewModels

/// <summary>
/// Sample ViewModel with properties for testing.
/// </summary>
public class SampleViewModelWithProperties : ObservableObject
{
    private string _name = string.Empty;
    private int _value;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

/// <summary>
/// Sample ViewModel with commands for testing.
/// </summary>
public class SampleViewModelWithCommands : ObservableObject
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public RelayCommand SaveCommand { get; }
    public RelayCommand LoadCommand { get; }

    public SampleViewModelWithCommands()
    {
        SaveCommand = new RelayCommand(Save);
        LoadCommand = new RelayCommand(Load);
    }

    private void Save() { }
    private void Load() { }
}

#endregion

#region Testable Validator

/// <summary>
/// Testable version of ViewModelBindingValidator without MAUI dependencies.
/// </summary>
public class TestableViewModelBindingValidator
{
    private readonly ILogger<TestableViewModelBindingValidator> _logger;
    private readonly List<TestValidationResult> _results = [];

    public TestableViewModelBindingValidator(ILogger<TestableViewModelBindingValidator> logger)
    {
        _logger = logger;
    }

    public void ValidateViewModel<T>(
        string[] expectedProperties,
        string[] expectedCommands) where T : class
    {
        var type = typeof(T);
        var typeName = type.Name;

        _logger.LogInformation("Validating {TypeName}...", typeName);

        // Check properties
        var actualProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.Name.StartsWith("Logger") && p.GetMethod != null)
            .Select(p => p.Name)
            .Distinct()
            .ToList();

        var missingProperties = expectedProperties.Except(actualProperties).ToList();
        var extraProperties = actualProperties.Except(expectedProperties).Where(p => !p.Contains("Command")).ToList();

        if (missingProperties.Count > 0)
        {
            _results.Add(new TestValidationResult
            {
                ViewModelType = typeName,
                Category = "Missing Properties",
                Status = TestValidationStatus.Failed,
                Message = $"Missing properties: {string.Join(", ", missingProperties)}"
            });
        }

        if (extraProperties.Count > 0)
        {
            _results.Add(new TestValidationResult
            {
                ViewModelType = typeName,
                Category = "Extra Properties",
                Status = TestValidationStatus.Warning,
                Message = $"Extra properties (may be OK): {string.Join(", ", extraProperties)}"
            });
        }

        // Check commands
        var actualCommands = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name.EndsWith("Command"))
            .Select(p => p.Name)
            .ToList();

        var missingCommands = expectedCommands.Except(actualCommands).ToList();
        var extraCommands = actualCommands.Except(expectedCommands).ToList();

        if (missingCommands.Count > 0)
        {
            _results.Add(new TestValidationResult
            {
                ViewModelType = typeName,
                Category = "Missing Commands",
                Status = TestValidationStatus.Failed,
                Message = $"Missing commands: {string.Join(", ", missingCommands)}"
            });
        }

        if (extraCommands.Count > 0)
        {
            _results.Add(new TestValidationResult
            {
                ViewModelType = typeName,
                Category = "Extra Commands",
                Status = TestValidationStatus.Info,
                Message = $"Extra commands: {string.Join(", ", extraCommands)}"
            });
        }

        if (missingProperties.Count == 0 && missingCommands.Count == 0)
        {
            _results.Add(new TestValidationResult
            {
                ViewModelType = typeName,
                Category = "Validation",
                Status = TestValidationStatus.Passed,
                Message = "All required properties and commands are present"
            });
        }
    }

    public TestValidationReport GetReport()
    {
        return new TestValidationReport(_results);
    }

    public void Clear()
    {
        _results.Clear();
    }
}

public enum TestValidationStatus
{
    Passed,
    Failed,
    Warning,
    Info
}

public class TestValidationResult
{
    public string ViewModelType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TestValidationStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class TestValidationReport
{
    public List<TestValidationResult> Results { get; }
    public List<TestValidationResult> PassedResults => Results.Where(r => r.Status == TestValidationStatus.Passed).ToList();
    public List<TestValidationResult> FailedResults => Results.Where(r => r.Status == TestValidationStatus.Failed).ToList();
    public List<TestValidationResult> WarningResults => Results.Where(r => r.Status == TestValidationStatus.Warning).ToList();
    public List<TestValidationResult> InfoResults => Results.Where(r => r.Status == TestValidationStatus.Info).ToList();

    public int TotalCount => Results.Count;
    public int PassedCount => PassedResults.Count;
    public int FailedCount => FailedResults.Count;
    public int WarningCount => WarningResults.Count;
    public int InfoCount => InfoResults.Count;

    public bool HasErrors => FailedCount > 0;
    public bool IsValid => FailedCount == 0;

    public TestValidationReport(List<TestValidationResult> results)
    {
        Results = results;
    }
}

#endregion
