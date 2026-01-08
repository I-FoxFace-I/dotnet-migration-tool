using Xunit;

namespace MigrationTool.Wpf.StaUITests;

/// <summary>
/// Test collection for STA UI tests - ensures all tests run sequentially and in STA thread.
/// </summary>
[CollectionDefinition("STA UI Tests", DisableParallelization = true)]
public class StaTestCollection
{
    // This class is just used to define the collection
}
