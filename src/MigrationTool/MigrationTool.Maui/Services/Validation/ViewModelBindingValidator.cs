using System.Reflection;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace MigrationTool.Maui.Services.Validation;

/// <summary>
/// Validates that ViewModels have correct properties and commands for XAML bindings.
/// MAUI version - validates any ViewModel type.
/// </summary>
public class ViewModelBindingValidator
{
    private readonly ILogger<ViewModelBindingValidator> _logger;
    private readonly List<ValidationResult> _results = [];

    public ViewModelBindingValidator(ILogger<ViewModelBindingValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a ViewModel type against expected properties and commands
    /// </summary>
    public void ValidateViewModel<T>(
        string[] expectedProperties,
        string[] expectedCommands) where T : class
    {
        var type = typeof(T);
        var typeName = type.Name;

        _logger.LogInformation("Validating {TypeName}...", typeName);

        // Check properties - including CommunityToolkit.Mvvm generated properties
        var observablePropertyFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.Name.StartsWith('_') &&
                       Attribute.IsDefined(f, typeof(ObservablePropertyAttribute)))
            .Select(f => char.ToUpper(f.Name[1]) + f.Name[2..])
            .ToList();

        var actualProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !p.Name.StartsWith("Logger") && p.GetMethod != null)
            .Select(p => p.Name)
            .Union(observablePropertyFields)
            .Distinct()
            .ToList();

        var missingProperties = expectedProperties.Except(actualProperties).ToList();
        var extraProperties = actualProperties.Except(expectedProperties).Where(p => !p.Contains("Command")).ToList();

        if (missingProperties.Count > 0)
        {
            _results.Add(new ValidationResult
            {
                ViewModelType = typeName,
                Category = "Missing Properties",
                Status = ValidationStatus.Failed,
                Message = $"Missing properties: {string.Join(", ", missingProperties)}"
            });
        }

        if (extraProperties.Count > 0)
        {
            _results.Add(new ValidationResult
            {
                ViewModelType = typeName,
                Category = "Extra Properties",
                Status = ValidationStatus.Warning,
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
            _results.Add(new ValidationResult
            {
                ViewModelType = typeName,
                Category = "Missing Commands",
                Status = ValidationStatus.Failed,
                Message = $"Missing commands: {string.Join(", ", missingCommands)}"
            });
        }

        if (extraCommands.Count > 0)
        {
            _results.Add(new ValidationResult
            {
                ViewModelType = typeName,
                Category = "Extra Commands",
                Status = ValidationStatus.Info,
                Message = $"Extra commands: {string.Join(", ", extraCommands)}"
            });
        }

        if (missingProperties.Count == 0 && missingCommands.Count == 0)
        {
            _results.Add(new ValidationResult
            {
                ViewModelType = typeName,
                Category = "Validation",
                Status = ValidationStatus.Passed,
                Message = "All required properties and commands are present"
            });
        }
    }

    /// <summary>
    /// Validates all ViewModels in an assembly against their XAML bindings.
    /// Auto-discovers ViewModels by convention (classes ending with "ViewModel").
    /// </summary>
    public void ValidateAssembly(Assembly assembly)
    {
        var viewModelTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("ViewModel"))
            .ToList();

        _logger.LogInformation("Found {Count} ViewModels to validate", viewModelTypes.Count);

        foreach (var type in viewModelTypes)
        {
            ValidateViewModelType(type);
        }
    }

    /// <summary>
    /// Validates a specific ViewModel type by reflection.
    /// </summary>
    private void ValidateViewModelType(Type type)
    {
        var typeName = type.Name;
        _logger.LogInformation("Auto-validating {TypeName}...", typeName);

        // Get all observable properties (fields with [ObservableProperty])
        var observablePropertyFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.Name.StartsWith('_') &&
                       Attribute.IsDefined(f, typeof(ObservablePropertyAttribute)))
            .Select(f => new
            {
                FieldName = f.Name,
                PropertyName = char.ToUpper(f.Name[1]) + f.Name[2..],
                FieldType = f.FieldType
            })
            .ToList();

        // Get all public properties
        var publicProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetMethod != null)
            .ToList();

        // Get all commands (properties ending with "Command")
        var commands = publicProperties
            .Where(p => p.Name.EndsWith("Command"))
            .ToList();

        // Check if generated properties exist for observable fields
        foreach (var field in observablePropertyFields)
        {
            var generatedProperty = publicProperties.FirstOrDefault(p => p.Name == field.PropertyName);
            if (generatedProperty == null)
            {
                _results.Add(new ValidationResult
                {
                    ViewModelType = typeName,
                    Category = "Generated Property Missing",
                    Status = ValidationStatus.Failed,
                    Message = $"Field '{field.FieldName}' has [ObservableProperty] but generated property '{field.PropertyName}' not found"
                });
            }
            else
            {
                _results.Add(new ValidationResult
                {
                    ViewModelType = typeName,
                    Category = "Observable Property",
                    Status = ValidationStatus.Passed,
                    Message = $"Property '{field.PropertyName}' correctly generated from '{field.FieldName}'"
                });
            }
        }

        // Check commands have corresponding methods
        var relayCommandMethods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => Attribute.IsDefined(m, typeof(CommunityToolkit.Mvvm.Input.RelayCommandAttribute)))
            .ToList();

        foreach (var method in relayCommandMethods)
        {
            var expectedCommandName = method.Name.Replace("Async", "") + "Command";
            var commandProperty = commands.FirstOrDefault(c => c.Name == expectedCommandName);

            if (commandProperty == null)
            {
                _results.Add(new ValidationResult
                {
                    ViewModelType = typeName,
                    Category = "Generated Command Missing",
                    Status = ValidationStatus.Failed,
                    Message = $"Method '{method.Name}' has [RelayCommand] but generated command '{expectedCommandName}' not found"
                });
            }
            else
            {
                _results.Add(new ValidationResult
                {
                    ViewModelType = typeName,
                    Category = "Relay Command",
                    Status = ValidationStatus.Passed,
                    Message = $"Command '{expectedCommandName}' correctly generated from '{method.Name}'"
                });
            }
        }

        // Summary for this ViewModel
        var vmResults = _results.Where(r => r.ViewModelType == typeName).ToList();
        if (vmResults.All(r => r.Status != ValidationStatus.Failed))
        {
            _logger.LogInformation("✅ {TypeName}: All bindings valid", typeName);
        }
        else
        {
            _logger.LogWarning("❌ {TypeName}: Has binding issues", typeName);
        }
    }

    /// <summary>
    /// Gets the validation report
    /// </summary>
    public ValidationReport GetReport()
    {
        return new ValidationReport(_results);
    }

    /// <summary>
    /// Clears previous validation results
    /// </summary>
    public void Clear()
    {
        _results.Clear();
    }

    /// <summary>
    /// Shows the validation report in a MAUI alert dialog and logs it
    /// </summary>
    public async Task ShowReportAsync(ValidationReport report, Page? page = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ViewModel Binding Validation Report ===\n");
        sb.AppendLine($"Total: {report.TotalCount}");
        sb.AppendLine($"Passed: {report.PassedCount}");
        sb.AppendLine($"Failed: {report.FailedCount}");
        sb.AppendLine($"Warnings: {report.WarningCount}");
        sb.AppendLine($"Info: {report.InfoCount}\n");

        if (report.FailedResults.Count > 0)
        {
            sb.AppendLine("=== FAILED ===");
            foreach (var result in report.FailedResults)
            {
                sb.AppendLine($"[{result.ViewModelType}] {result.Category}: {result.Message}");
            }
            sb.AppendLine();
        }

        if (report.WarningResults.Count > 0)
        {
            sb.AppendLine("=== WARNINGS ===");
            foreach (var result in report.WarningResults)
            {
                sb.AppendLine($"[{result.ViewModelType}] {result.Category}: {result.Message}");
            }
            sb.AppendLine();
        }

        if (report.PassedResults.Count > 0)
        {
            sb.AppendLine("=== PASSED ===");
            foreach (var result in report.PassedResults)
            {
                sb.AppendLine($"[{result.ViewModelType}] {result.Message}");
            }
        }

        var message = sb.ToString();
        _logger.LogInformation("{Report}", message);

        // Show MAUI alert if page is available
        if (page != null)
        {
            var title = report.FailedCount > 0
                ? "⚠️ Binding Validation Issues"
                : "✅ Binding Validation Passed";

            await page.DisplayAlert(title, message, "OK");
        }
    }

    /// <summary>
    /// Logs the report to console/debug output
    /// </summary>
    public void LogReport(ValidationReport report)
    {
        _logger.LogInformation("=== ViewModel Binding Validation Report ===");
        _logger.LogInformation("Total: {Total}, Passed: {Passed}, Failed: {Failed}, Warnings: {Warnings}",
            report.TotalCount, report.PassedCount, report.FailedCount, report.WarningCount);

        foreach (var result in report.FailedResults)
        {
            _logger.LogError("[{ViewModelType}] {Category}: {Message}",
                result.ViewModelType, result.Category, result.Message);
        }

        foreach (var result in report.WarningResults)
        {
            _logger.LogWarning("[{ViewModelType}] {Category}: {Message}",
                result.ViewModelType, result.Category, result.Message);
        }

        foreach (var result in report.PassedResults)
        {
            _logger.LogInformation("[{ViewModelType}] ✅ {Message}",
                result.ViewModelType, result.Message);
        }
    }
}

public enum ValidationStatus
{
    Passed,
    Failed,
    Warning,
    Info
}

public class ValidationResult
{
    public string ViewModelType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ValidationStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ValidationReport
{
    public List<ValidationResult> Results { get; }
    public List<ValidationResult> PassedResults => Results.Where(r => r.Status == ValidationStatus.Passed).ToList();
    public List<ValidationResult> FailedResults => Results.Where(r => r.Status == ValidationStatus.Failed).ToList();
    public List<ValidationResult> WarningResults => Results.Where(r => r.Status == ValidationStatus.Warning).ToList();
    public List<ValidationResult> InfoResults => Results.Where(r => r.Status == ValidationStatus.Info).ToList();

    public int TotalCount => Results.Count;
    public int PassedCount => PassedResults.Count;
    public int FailedCount => FailedResults.Count;
    public int WarningCount => WarningResults.Count;
    public int InfoCount => InfoResults.Count;

    public bool HasErrors => FailedCount > 0;
    public bool IsValid => FailedCount == 0;

    public ValidationReport(List<ValidationResult> results)
    {
        Results = results;
    }
}
