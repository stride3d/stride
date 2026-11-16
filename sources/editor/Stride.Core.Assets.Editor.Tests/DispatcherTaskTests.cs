// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Where a test goes on after it awaits a <see cref="DispatcherTask{TResult}"/>.
/// </summary>
public sealed class DispatcherTaskTests
{
    [Fact]
    public async Task AnOpenThatEndedBeforeTheTestAwaitsItContinuesTheTestInTheDispatcher()
    {
        var open = new TaskCompletionSource<int>();
        var opening = DispatcherTask.InDispatcher(() => open.Task);
        ExclusiveContext.Run(() => open.TrySetResult(1));

        Assert.Equal(1, await opening);
        Assert.True(ExclusiveContext.IsEntered);
    }

    [Fact]
    public async Task AnOpenThatEndsAtOnceContinuesTheTestInTheDispatcher()
    {
        Assert.Equal(1, await DispatcherTask.InDispatcher(() => Task.FromResult(1)));
        Assert.True(ExclusiveContext.IsEntered);
    }

    [Fact]
    public async Task AHelperThatWrapsAnOpenContinuesTheTestInTheDispatcher()
    {
        var open = new TaskCompletionSource<int>();
        var opening = WrapAsync(open.Task);
        ExclusiveContext.Run(() => open.TrySetResult(1));

        Assert.Equal(2, await opening);
        Assert.True(ExclusiveContext.IsEntered);
    }

    private static async DispatcherTask<int> WrapAsync(Task<int> open) => await DispatcherTask.InDispatcher(() => open) + 1;
}
