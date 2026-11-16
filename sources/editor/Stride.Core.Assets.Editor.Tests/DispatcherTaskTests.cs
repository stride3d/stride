// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Where a test goes on after it awaits a <see cref="DispatcherTask{TResult}"/>.
/// </summary>
public sealed class DispatcherTaskTests
{
    private readonly ExclusiveDispatcherService dispatcher = new();

    [Fact]
    public async Task AnOpenThatEndedBeforeTheTestAwaitsItContinuesTheTestInTheDispatcher()
    {
        var open = new TaskCompletionSource<int>();
        var opening = DispatcherTask.InDispatcher(() => open.Task);
        dispatcher.Invoke(() => open.TrySetResult(1));

        Assert.Equal(1, await opening);
        Assert.True(dispatcher.CheckAccess());
    }

    [Fact]
    public async Task AnOpenThatEndsAtOnceContinuesTheTestInTheDispatcher()
    {
        Assert.Equal(1, await DispatcherTask.InDispatcher(() => Task.FromResult(1)));
        Assert.True(dispatcher.CheckAccess());
    }

    [Fact]
    public async Task AHelperThatWrapsAnOpenContinuesTheTestInTheDispatcher()
    {
        var open = new TaskCompletionSource<int>();
        var opening = WrapAsync(open.Task);
        dispatcher.Invoke(() => open.TrySetResult(1));

        Assert.Equal(2, await opening);
        Assert.True(dispatcher.CheckAccess());
    }

    private static async DispatcherTask<int> WrapAsync(Task<int> open) => await DispatcherTask.InDispatcher(() => open) + 1;
}
