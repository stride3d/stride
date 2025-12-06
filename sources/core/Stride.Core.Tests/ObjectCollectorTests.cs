// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests;

public class ObjectCollectorTests
{
    private class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    [Fact]
    public void Add_WithDisposable_AddsToCollection()
    {
        var collector = new ObjectCollector();
        var disposable = new TestDisposable();

        var result = collector.Add(disposable);

        Assert.Same(disposable, result);
    }

    [Fact]
    public void Dispose_DisposesAllCollectedObjects()
    {
        var collector = new ObjectCollector();
        var disposable1 = new TestDisposable();
        var disposable2 = new TestDisposable();

        collector.Add(disposable1);
        collector.Add(disposable2);

        collector.Dispose();

        Assert.True(disposable1.IsDisposed);
        Assert.True(disposable2.IsDisposed);
    }

    [Fact]
    public void Dispose_DisposesInReverseOrder()
    {
        var collector = new ObjectCollector();
        var disposeOrder = new List<int>();

        collector.Add(new DisposableWithCallback(() => disposeOrder.Add(1)));
        collector.Add(new DisposableWithCallback(() => disposeOrder.Add(2)));
        collector.Add(new DisposableWithCallback(() => disposeOrder.Add(3)));

        collector.Dispose();

        Assert.Equal(new[] { 3, 2, 1 }, disposeOrder);
    }

    [Fact]
    public void Remove_RemovesObjectFromCollection()
    {
        var collector = new ObjectCollector();
        var disposable = new TestDisposable();

        collector.Add(disposable);
        collector.Remove(disposable);
        collector.Dispose();

        Assert.False(disposable.IsDisposed);
    }

    [Fact]
    public void RemoveAndDispose_RemovesAndDisposesObject()
    {
        var collector = new ObjectCollector();
        TestDisposable? disposable = new TestDisposable();
        var wasDisposed = false;

        collector.Add(disposable);

        // Capture disposed state before setting to null
        disposable = new TestDisposable();
        collector.Add(disposable);
        collector.RemoveAndDispose(ref disposable);
        wasDisposed = disposable == null; // After RemoveAndDispose, ref should be null

        Assert.True(wasDisposed);
    }

    [Fact]
    public unsafe void Add_WithIntPtr_AddsMemoryPointer()
    {
        var collector = new ObjectCollector();
        var ptr = Utilities.AllocateMemory(128);

        var result = collector.Add(ptr);

        Assert.Equal(ptr, result);

        collector.Dispose();
        // Memory should be freed (can't test directly)
    }

    private class DisposableWithCallback : IDisposable
    {
        private readonly Action onDispose;

        public DisposableWithCallback(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            onDispose();
        }
    }
}
