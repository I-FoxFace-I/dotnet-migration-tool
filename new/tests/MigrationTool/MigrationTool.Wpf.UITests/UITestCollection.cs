using Xunit;

namespace MigrationTool.Wpf.UITests;

/// <summary>
/// Collection definition for UI tests.
/// All UI tests run sequentially to avoid conflicts.
/// </summary>
[CollectionDefinition("UI Tests", DisableParallelization = true)]
public class UITestCollection
{
}
