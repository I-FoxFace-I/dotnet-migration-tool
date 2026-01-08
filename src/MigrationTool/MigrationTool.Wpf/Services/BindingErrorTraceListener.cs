using System.Diagnostics;
using System.Text;
using System.Windows;

namespace MigrationTool.Wpf.Services;

/// <summary>
/// TraceListener that captures WPF binding errors.
/// In DEBUG mode, collects errors and can show them in a dialog.
/// </summary>
public class BindingErrorTraceListener : TraceListener
{
    private readonly StringBuilder _errors = new();
    private int _errorCount;

    /// <summary>
    /// Gets the number of binding errors detected.
    /// </summary>
    public int ErrorCount => _errorCount;

    /// <summary>
    /// Gets whether any binding errors were detected.
    /// </summary>
    public bool HasErrors => _errorCount > 0;

    /// <summary>
    /// Gets all collected binding errors as a string.
    /// </summary>
    public string GetErrors() => _errors.ToString();

    public override void Write(string? message)
    {
        if (string.IsNullOrEmpty(message)) return;

        _errors.AppendLine(message);
        Debug.WriteLine($"[BINDING ERROR] {message}");
    }

    public override void WriteLine(string? message)
    {
        if (string.IsNullOrEmpty(message)) return;

        _errorCount++;
        _errors.AppendLine($"[{_errorCount}] {message}");
        _errors.AppendLine();

        Debug.WriteLine($"[BINDING ERROR #{_errorCount}] {message}");
    }

    /// <summary>
    /// Shows a dialog with all collected binding errors.
    /// Call this after the main window is loaded to see startup binding issues.
    /// </summary>
    public void ShowErrorsDialog()
    {
        if (!HasErrors)
        {
            MessageBox.Show(
                "✅ No binding errors detected!",
                "Binding Validation",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var message = $"⚠️ {_errorCount} binding error(s) detected:\n\n{GetErrors()}";

        MessageBox.Show(
            message,
            "Binding Errors Detected",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    /// <summary>
    /// Clears all collected errors.
    /// </summary>
    public void Clear()
    {
        _errors.Clear();
        _errorCount = 0;
    }
}
