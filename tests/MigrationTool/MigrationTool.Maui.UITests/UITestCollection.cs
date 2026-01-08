using Xunit;

namespace MigrationTool.Maui.UITests;

/// <summary>
/// Collection definition for UI tests.
/// Ensures tests run sequentially (one app instance at a time).
/// </summary>
[CollectionDefinition("UI Tests")]
public class UITestCollection : ICollectionFixture<UITestCollectionFixture>
{
}

/// <summary>
/// Fixture for UI test collection - shared setup/teardown.
/// </summary>
public class UITestCollectionFixture : IDisposable
{
    public UITestCollectionFixture()
    {
        // Collection-level setup if needed
    }

    public void Dispose()
    {
        // Collection-level cleanup if needed
    }
}
