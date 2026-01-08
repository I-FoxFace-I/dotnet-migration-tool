using System.CommandLine;
using MigrationTool.Cli.Utilities;
using MigrationTool.Core.Services;

namespace MigrationTool.Cli.Commands;

/// <summary>
/// Command to find all usages of a symbol in a solution.
/// </summary>
public static class FindUsagesCommand
{
    /// <summary>
    /// Creates the find-usages command.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("find-usages", "Find all usages of a symbol");
        
        var pathOption = new Option<string>("--path", "Path to .sln file") { IsRequired = true };
        var symbolOption = new Option<string>("--symbol", "Symbol name to find") { IsRequired = true };
        
        command.AddOption(pathOption);
        command.AddOption(symbolOption);
        
        command.SetHandler(ExecuteAsync, pathOption, symbolOption);
        
        return command;
    }

    private static async Task ExecuteAsync(string solutionPath, string symbol)
    {
        try
        {
            var service = new SymbolUsageService();
            var result = await service.FindUsagesAsync(solutionPath, symbol);
            
            JsonOutput.WriteSuccess(new
            {
                result.Symbol,
                result.Count,
                Usages = result.Usages.Select(u => new
                {
                    File = u.FilePath,
                    u.Line,
                    u.Column
                })
            });
        }
        catch (Exception ex)
        {
            JsonOutput.WriteException(ex);
        }
    }
}
