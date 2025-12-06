// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.ReferenceCounting;
using Xunit;

namespace Stride.Core.Tests;

public class DisposeBaseTests
{
    private class TestDisposable : DisposeBase
    {
        public bool IsDestroyCalled { get; private set; }

        protected override void Destroy()
        {
            IsDestroyCalled = true;
            base.Destroy();
        }
    }

    [Fact]
    public void Constructor_InitializesWithReferenceCountOfOne()
    {
        var obj = new TestDisposable();

        Assert.Equal(1, ((IReferencable)obj).ReferenceCount);
        Assert.False(obj.IsDisposed);
    }

    [Fact]
    public void Dispose_CallsDestroy()
    {
        var obj = new TestDisposable();

        obj.Dispose();

        Assert.True(obj.IsDestroyCalled);
        Assert.True(obj.IsDisposed);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_CallsDestroyOnlyOnce()
    {
        var obj = new TestDisposable();

        obj.Dispose();
        var firstCallResult = obj.IsDestroyCalled;
        obj.Dispose(); // Second dispose

        Assert.True(firstCallResult);
        Assert.True(obj.IsDisposed);
    }

    [Fact]
    public void AddReference_IncrementsCounter()
    {
        var obj = new TestDisposable();
        var referencable = (IReferencable)obj;

        var newCount = referencable.AddReference();

        Assert.Equal(2, newCount);
        Assert.Equal(2, referencable.ReferenceCount);
    }

    [Fact]
    public void AddReference_OnDisposedObject_ThrowsInvalidOperationException()
    {
        var obj = new TestDisposable();
        obj.Dispose();
        var referencable = (IReferencable)obj;

        Assert.Throws<InvalidOperationException>(() => referencable.AddReference());
    }

    [Fact]
    public void Release_DecrementsCounter()
    {
        var obj = new TestDisposable();
        var referencable = (IReferencable)obj;
        referencable.AddReference(); // Count is now 2

        var newCount = referencable.Release();

        Assert.Equal(1, newCount);
        Assert.Equal(1, referencable.ReferenceCount);
        Assert.False(obj.IsDisposed);
    }

    [Fact]
    public void Release_WhenCountReachesZero_DisposesObject()
    {
        var obj = new TestDisposable();
        var referencable = (IReferencable)obj;

        var newCount = referencable.Release();

        Assert.Equal(0, newCount);
        Assert.True(obj.IsDestroyCalled);
        Assert.True(obj.IsDisposed);
    }

    [Fact]
    public void Release_BelowZero_ThrowsInvalidOperationException()
    {
        var obj = new TestDisposable();
        var referencable = (IReferencable)obj;
        referencable.Release(); // Goes to 0 and disposes

        Assert.Throws<InvalidOperationException>(() => referencable.Release());
    }

    [Fact]
    public void ReferenceCountingWorkflow_WorksCorrectly()
    {
        var obj = new TestDisposable();
        var referencable = (IReferencable)obj;

        // Add multiple references
        referencable.AddReference(); // 2
        referencable.AddReference(); // 3
        Assert.Equal(3, referencable.ReferenceCount);

        // Release some
        referencable.Release(); // 2
        Assert.False(obj.IsDisposed);

        referencable.Release(); // 1
        Assert.False(obj.IsDisposed);

        referencable.Release(); // 0 - should dispose
        Assert.True(obj.IsDisposed);
    }
}
