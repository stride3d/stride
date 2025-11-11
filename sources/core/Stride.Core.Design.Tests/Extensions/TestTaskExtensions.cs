// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for <see cref="TaskExtensions"/> class.
/// </summary>
public class TestTaskExtensions
{
    [Fact]
    public void Forget_WithValidTask_DoesNotThrow()
    {
        var task = Task.CompletedTask;
        task.Forget(); // Should not throw
    }

    [Fact]
    public void Forget_WithNull_ThrowsArgumentNullException()
    {
        Task? task = null;
        Assert.Throws<ArgumentNullException>(() => task!.Forget());
    }

    [Fact]
    public async Task Forget_WithRunningTask_DoesNotAwait()
    {
        var tcs = new TaskCompletionSource();
        var task = tcs.Task;

        // Call Forget - should not block
        task.Forget();

        // Complete the task afterwards
        tcs.SetResult();
        await task; // Verify task can still be awaited elsewhere
    }
}
