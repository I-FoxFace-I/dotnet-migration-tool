namespace MigrationTool.Core.Abstractions.Services;

/// <summary>
/// Service for finding symbol usages in a solution.
/// </summary>
public interface ISymbolUsageService
{
    /// <summary>
    /// Finds all usages of a symbol in a solution.
    /// </summary>
    /// <param name="solutionPath">Path to the .sln file.</param>
    /// <param name="symbol">Symbol name to find.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing all found usages.</returns>
    Task<SymbolUsageResult> FindUsagesAsync(
        string solutionPath, 
        string symbol,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a symbol usage search.
/// </summary>
public record SymbolUsageResult(
    bool Success,
    string Symbol,
    int Count,
    IReadOnlyList<SymbolUsage> Usages,
    string? ErrorMessage = null);

/// <summary>
/// Represents a single usage of a symbol.
/// </summary>
public record SymbolUsage(
    string FilePath,
    int Line,
    int Column);
