// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Stride.Packaging.Tests.ParallelTestFramework", "Stride.Packaging.Tests")]

namespace Stride.Packaging.Tests;

/// <summary>
/// Runs the test methods of a collection at the same time, at most <see cref="MaxParallelTests"/> at once: xUnit runs
/// the tests of a collection one after the other.
/// </summary>
/// <remarks>
/// The tests spend their time in <c>dotnet</c> child processes. The work they do in the test process takes
/// <see cref="TestEnvironment.InProcessLock"/>.
/// </remarks>
public sealed class ParallelTestFramework(IMessageSink messageSink) : XunitTestFramework(messageSink)
{
    /// <summary>The number of tests at once, from STRIDE_PACKAGING_TESTS_PARALLEL; one when it is not set.</summary>
    public static int MaxParallelTests { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("STRIDE_PACKAGING_TESTS_PARALLEL"), out var count) && count > 0 ? count : 1;

    private static readonly SemaphoreSlim Slots = new(MaxParallelTests);

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new Executor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);

    private sealed class Executor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink)
        : XunitTestFrameworkExecutor(assemblyName, sourceInformationProvider, diagnosticMessageSink)
    {
        protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using var runner = new AssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
            await runner.RunAsync();
        }
    }

    private sealed class AssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        : XunitTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
    {
        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
            => new CollectionRunner(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), cancellationTokenSource).RunAsync();
    }

    private sealed class CollectionRunner(ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        : XunitTestCollectionRunner(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
    {
        protected override async Task<RunSummary> RunTestClassesAsync()
        {
            var classes = TestCases.GroupBy(tc => tc.TestMethod.TestClass, TestClassComparer.Instance)
                .Select(group => RunTestClassAsync(group.Key, (IReflectionTypeInfo)group.Key.Class, group));
            return Sum(await Task.WhenAll(classes));
        }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
            => new ClassRunner(testClass, @class, testCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer, new ExceptionAggregator(Aggregator), CancellationTokenSource, CollectionFixtureMappings).RunAsync();
    }

    private sealed class ClassRunner(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource, IDictionary<Type, object> collectionFixtureMappings)
        : XunitTestClassRunner(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
    {
        protected override async Task<RunSummary> RunTestMethodsAsync()
        {
            var methods = TestCases.GroupBy(tc => tc.TestMethod, TestMethodComparer.Instance)
                .Select(group => RunInSlotAsync(group.Key, group.ToList()));
            return Sum(await Task.WhenAll(methods));
        }

        private async Task<RunSummary> RunInSlotAsync(ITestMethod method, IReadOnlyList<IXunitTestCase> testCases)
        {
            await Slots.WaitAsync(CancellationTokenSource.Token);
            try
            {
                // Arguments of its own: the ITestOutputHelper among them belongs to one test at a time. The test
                // methods block their thread on child processes: Task.Run keeps them off the threads of the runner.
                var arguments = CreateTestClassConstructorArguments();
                return await Task.Run(() => RunTestMethodAsync(method, (IReflectionMethodInfo)method.Method, testCases, arguments));
            }
            finally
            {
                Slots.Release();
            }
        }
    }

    private static RunSummary Sum(IEnumerable<RunSummary> summaries)
    {
        var total = new RunSummary();
        foreach (var summary in summaries)
            total.Aggregate(summary);
        return total;
    }
}
