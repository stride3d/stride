// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

[assembly: TestFramework(typeof(Stride.Packaging.Tests.ParallelTestFramework))]

namespace Stride.Packaging.Tests;

/// <summary>
/// Runs the test methods of a class at the same time, at most <see cref="MaxParallelTests"/> at once over all the
/// classes: xUnit runs the classes at the same time, but the tests of a class one after the other.
/// </summary>
/// <remarks>
/// The tests spend their time in <c>dotnet</c> child processes. The work they do in the test process takes
/// <see cref="TestEnvironment.InProcessLock"/>.
/// </remarks>
public sealed class ParallelTestFramework : XunitTestFramework
{
    /// <summary>The number of tests at once, from STRIDE_PACKAGING_TESTS_PARALLEL; one when it is not set.</summary>
    public static int MaxParallelTests { get; } =
        int.TryParse(Environment.GetEnvironmentVariable("STRIDE_PACKAGING_TESTS_PARALLEL"), out var count) && count > 0 ? count : 1;

    private static readonly SemaphoreSlim Slots = new(MaxParallelTests);

    protected override ITestFrameworkExecutor CreateExecutor(Assembly assembly)
        => new Executor(new XunitTestAssembly(assembly, version: assembly.GetName().Version));

    private sealed class Executor(IXunitTestAssembly testAssembly) : XunitTestFrameworkExecutor(testAssembly)
    {
        public override async ValueTask RunTestCases(IReadOnlyCollection<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions, CancellationToken cancellationToken)
            => await AssemblyRunner.Instance.Run(TestAssembly, testCases, executionMessageSink, executionOptions, cancellationToken);
    }

    // xUnit's runners are singletons that call each other's Instance: each level is replaced to reach ClassRunner
    private sealed class AssemblyRunner : XunitTestAssemblyRunner
    {
        public static new AssemblyRunner Instance { get; } = new();

        protected override ValueTask<RunSummary> RunTestCollection(XunitTestAssemblyRunnerContext ctxt, IXunitTestCollection testCollection, IReadOnlyCollection<IXunitTestCase> testCases)
            => CollectionRunner.Instance.Run(testCollection, testCases, ctxt.ExplicitOption, ctxt.MessageBus, ctxt.AssemblyTestCaseOrderer ?? DefaultTestCaseOrderer.Instance,
                ctxt.Aggregator.Clone(), ctxt.CancellationTokenSource, ctxt.AssemblyFixtureMappings);
    }

    private sealed class CollectionRunner : XunitTestCollectionRunner
    {
        public static new CollectionRunner Instance { get; } = new();

        protected override ValueTask<RunSummary> RunTestClass(XunitTestCollectionRunnerContext ctxt, IXunitTestClass? testClass, IReadOnlyCollection<IXunitTestCase> testCases)
            => testClass is null
                ? base.RunTestClass(ctxt, testClass, testCases)
                : ClassRunner.Instance.Run(testClass, testCases, ctxt.ExplicitOption, ctxt.MessageBus, ctxt.TestCaseOrderer, ctxt.Aggregator.Clone(), ctxt.CancellationTokenSource, ctxt.CollectionFixtureMappings);
    }

    private sealed class ClassRunner : XunitTestClassRunner
    {
        public static new ClassRunner Instance { get; } = new();

        protected override async ValueTask<RunSummary> RunTestMethods(XunitTestClassRunnerContext ctxt, Exception? exception)
        {
            if (exception is null)
            {
                var testCases = OrderTestCases(ctxt);
                // The ITestOutputHelper among them is the one of the current test, so the tests can share them
                var constructorArguments = await CreateTestClassConstructorArguments(ctxt);
                exception = ctxt.Aggregator.ToException();
                ctxt.Aggregator.Clear();
                if (exception is null)
                {
                    var methods = testCases.GroupBy<IXunitTestCase, IXunitTestMethod>(x => x.TestMethod, TestMethodComparer.Instance)
                        .Select(group => RunInSlot(ctxt, group.Key, group.ToList(), constructorArguments));
                    return Sum(await Task.WhenAll(methods));
                }
            }
            // Fails the tests of the class with the exception
            return await base.RunTestMethods(ctxt, exception);
        }

        private async Task<RunSummary> RunInSlot(XunitTestClassRunnerContext ctxt, IXunitTestMethod method, IReadOnlyCollection<IXunitTestCase> testCases, object?[] constructorArguments)
        {
            await Slots.WaitAsync(ctxt.CancellationTokenSource.Token);
            try
            {
                // The test methods block their thread on child processes: Task.Run keeps them off the threads of the runner
                return await Task.Run(() => RunTestMethod(ctxt, method, testCases, constructorArguments).AsTask());
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
