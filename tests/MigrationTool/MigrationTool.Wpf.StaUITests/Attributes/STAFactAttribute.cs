using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace MigrationTool.Wpf.StaUITests.Attributes;

/// <summary>
/// Custom xUnit fact attribute that runs tests on an STA thread.
/// Required for WPF UI tests that create windows or work with UI elements.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("MigrationTool.Wpf.StaUITests.Attributes.STAFactDiscoverer", "MigrationTool.Wpf.StaUITests")]
public sealed class STAFactAttribute : FactAttribute
{
}

/// <summary>
/// Discoverer for STAFact tests.
/// </summary>
public class STAFactDiscoverer : IXunitTestCaseDiscoverer
{
    private readonly IMessageSink _diagnosticMessageSink;

    public STAFactDiscoverer(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        yield return new STATestCase(
            _diagnosticMessageSink,
            discoveryOptions.MethodDisplayOrDefault(),
            discoveryOptions.MethodDisplayOptionsOrDefault(),
            testMethod);
    }
}

/// <summary>
/// Test case that runs on an STA thread.
/// </summary>
public class STATestCase : XunitTestCase
{
    [Obsolete("Required for deserialization")]
    public STATestCase() { }

    public STATestCase(
        IMessageSink diagnosticMessageSink,
        TestMethodDisplay defaultMethodDisplay,
        TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod,
        object[]? testMethodArguments = null)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
    {
    }

    public override Task<RunSummary> RunAsync(
        IMessageSink diagnosticMessageSink,
        IMessageBus messageBus,
        object[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource)
    {
        var tcs = new TaskCompletionSource<RunSummary>();
        var thread = new Thread(() =>
        {
            try
            {
                var task = base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
                tcs.SetResult(task.GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        
        return tcs.Task;
    }
}

/// <summary>
/// Custom xUnit theory attribute that runs tests on an STA thread.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("MigrationTool.Wpf.StaUITests.Attributes.STATheoryDiscoverer", "MigrationTool.Wpf.StaUITests")]
public sealed class STATheoryAttribute : TheoryAttribute
{
}

/// <summary>
/// Discoverer for STATheory tests.
/// </summary>
public class STATheoryDiscoverer : IXunitTestCaseDiscoverer
{
    private readonly IMessageSink _diagnosticMessageSink;
    private readonly TheoryDiscoverer _theoryDiscoverer;

    public STATheoryDiscoverer(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
        _theoryDiscoverer = new TheoryDiscoverer(diagnosticMessageSink);
    }

    public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return _theoryDiscoverer.Discover(discoveryOptions, testMethod, factAttribute)
            .Select(testCase => new STATestCase(
                _diagnosticMessageSink,
                discoveryOptions.MethodDisplayOrDefault(),
                discoveryOptions.MethodDisplayOptionsOrDefault(),
                testCase.TestMethod,
                testCase.TestMethodArguments));
    }
}
