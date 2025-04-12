// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Xunit.Abstractions;

namespace xunit.runner.stride.ViewModels;

public sealed class XSink : IExecutionSink
{
    volatile int errors;

    public ManualResetEvent Finished { get; } = new ManualResetEvent(initialState: false);

    public ExecutionSummary ExecutionSummary { get; } = new ExecutionSummary();

    public void Dispose()
    {
    }

    public bool OnMessageWithTypes(IMessageSinkMessage message, HashSet<string> messageTypes)
    {
        Console.WriteLine($"{message.GetType().Name} ... {message}");

        return message.Dispatch<ITestCaseFinished>(messageTypes, HandleTestCaseFinished)
            && message.Dispatch<ITestCaseStarting>(messageTypes, HandleTestCaseStarting)
            && message.Dispatch<IErrorMessage>(messageTypes, _ => Interlocked.Increment(ref errors))
            && message.Dispatch<ITestAssemblyCleanupFailure>(messageTypes, _ => Interlocked.Increment(ref errors))
            && message.Dispatch<ITestAssemblyFinished>(messageTypes, HandleTestAssemblyFinished)
            && message.Dispatch<ITestCaseCleanupFailure>(messageTypes, _ => Interlocked.Increment(ref errors))
            && message.Dispatch<ITestClassCleanupFailure>(messageTypes, _ => Interlocked.Increment(ref errors))
            && message.Dispatch<ITestCleanupFailure>(messageTypes, _ => Interlocked.Increment(ref errors))
            && message.Dispatch<ITestCollectionCleanupFailure>(messageTypes, _ => Interlocked.Increment(ref errors))
            && message.Dispatch<ITestMethodCleanupFailure>(messageTypes, _ => Interlocked.Increment(ref errors));
    }

    public MessageHandler<ITestCaseFinished>? HandleTestCaseFinished;
    public MessageHandler<ITestCaseStarting>? HandleTestCaseStarting;

    void HandleTestAssemblyFinished(MessageHandlerArgs<ITestAssemblyFinished> args)
    {
        ExecutionSummary.Total = args.Message.TestsRun;
        ExecutionSummary.Failed = args.Message.TestsFailed;
        ExecutionSummary.Skipped = args.Message.TestsSkipped;
        ExecutionSummary.Time = args.Message.ExecutionTime;
        ExecutionSummary.Errors = errors;

        Finished.Set();
    }
}
