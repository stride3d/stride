// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace xunit.runner.stride;

/// <summary>
/// In-process xunit v2 discovery and execution over an already-loaded assembly. Required on
/// Android where assemblies are bundled and <see cref="Assembly.Location"/> is empty. Keeping
/// it in-process on every platform also preserves statics like
/// <c>GameTestBase.ForceInteractiveMode</c> across discovery and run.
/// </summary>
internal sealed class StrideTestController : IDisposable
{
    private readonly XunitTestFrameworkDiscoverer discoverer;
    private readonly XunitTestFrameworkExecutor executor;
    private readonly NullSourceInformationProvider sourceInformationProvider = new();

    public StrideTestController(Assembly testAssembly)
    {
        var diagnosticMessageSink = new Xunit.Sdk.NullMessageSink();
        discoverer = new XunitTestFrameworkDiscoverer(new ReflectionAssemblyInfo(testAssembly), sourceInformationProvider, diagnosticMessageSink);
        executor = new XunitTestFrameworkExecutor(testAssembly.GetName(), sourceInformationProvider, diagnosticMessageSink);
    }

    // Callers pass the runner's *WithTypes sinks; MessageSinkAdapter.Wrap bridges them to the
    // Xunit.Abstractions.IMessageSink the discoverer/executor expect.
    public void Find(bool includeSourceInformation, IMessageSinkWithTypes messageSink, ITestFrameworkDiscoveryOptions discoveryOptions)
        => discoverer.Find(includeSourceInformation, MessageSinkAdapter.Wrap(messageSink), discoveryOptions);

    public void RunTests(IEnumerable<ITestCase> testCases, IMessageSinkWithTypes messageSink, ITestFrameworkExecutionOptions executionOptions)
        => executor.RunTests(testCases, MessageSinkAdapter.Wrap(messageSink), executionOptions);

    public void Dispose()
    {
        discoverer.Dispose();
        executor.Dispose();
        sourceInformationProvider.Dispose();
    }
}
