// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Testing;

/// <summary>
/// The sessions of the kit open in the process, each with the test that opened it: a session that opens while another test has one
/// open fails at once, since the two would share the dispatcher and the editor's process-wide registries and hang.
/// </summary>
/// <remarks>
/// A test is told from another by its flow (its <see cref="ExecutionContext"/>, which follows its awaits and the tasks it starts). A
/// test that already goes on in the exclusive dispatcher counts as the test of the sessions there: a helper that opens a session
/// in an <c>async</c> method opens it in a flow of its own, and its caller then goes on in the dispatcher.
/// </remarks>
internal static class OpenSessions
{
    internal const string ParallelRunsMessage =
        "Another test has a session of the editor test kit open. The sessions share one dispatcher and the editor's process-wide registries,"
        + " so tests that open them can't run at the same time: turn off parallel runs in the test project, for xUnit with"
        + " [assembly: CollectionBehavior(DisableTestParallelization = true)] (other test frameworks have the same setting)."
        + " If the tests don't run in parallel, an earlier test did not dispose its session (await using).";

    private static readonly AsyncLocal<object?> TestFlow = new();
    private static readonly Lock Gate = new();
    private static readonly List<Registration> Sessions = [];

    /// <summary>
    /// Records a session that starts to open in the current test.
    /// </summary>
    /// <returns>The registration of the session, to dispose once it is closed (or failed to open).</returns>
    /// <exception cref="InvalidOperationException">Another test has a session open.</exception>
    public static IDisposable Add()
    {
        var flow = TestFlow.Value ??= new object();
        var inDispatcher = ExclusiveContext.IsEntered;
        lock (Gate)
        {
            if (!inDispatcher && Sessions.Any(x => x.Flow != flow))
                throw new InvalidOperationException(ParallelRunsMessage);
            var registration = new Registration(flow);
            Sessions.Add(registration);
            return registration;
        }
    }

    private sealed class Registration(object flow) : IDisposable
    {
        public object Flow { get; } = flow;

        public void Dispose()
        {
            lock (Gate)
                Sessions.Remove(this);
        }
    }
}
