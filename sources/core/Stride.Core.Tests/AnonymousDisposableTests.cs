// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests;

public class AnonymousDisposableTests
{
    [Fact]
    public void Constructor_WithNullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AnonymousDisposable(null!));
    }

    [Fact]
    public void Dispose_InvokesAction()
    {
        var disposed = false;
        var disposable = new AnonymousDisposable(() => disposed = true);

        disposable.Dispose();

        Assert.True(disposed);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_InvokesActionOnlyOnce()
    {
        var disposeCount = 0;
        var disposable = new AnonymousDisposable(() => disposeCount++);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_WithExceptionInAction_PropagatesException()
    {
        var disposable = new AnonymousDisposable(() => throw new InvalidOperationException("Test exception"));

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());
    }

    [Fact]
    public void Dispose_WithUsingStatement_InvokesAction()
    {
        var disposed = false;

        using (new AnonymousDisposable(() => disposed = true))
        {
            Assert.False(disposed);
        }

        Assert.True(disposed);
    }
}
