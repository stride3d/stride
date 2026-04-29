// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Tests;

public class ComponentBaseTests
{
    private class TestComponent : ComponentBase
    {
        public TestComponent() : base() { }
        public TestComponent(string name) : base(name) { }
    }

    [Fact]
    public void Constructor_InitializesWithTypeName()
    {
        var component = new TestComponent();

        Assert.Equal(nameof(TestComponent), component.Name);
    }

    [Fact]
    public void Constructor_WithName_SetsName()
    {
        var component = new TestComponent("MyComponent");

        Assert.Equal("MyComponent", component.Name);
    }

    [Fact]
    public void Tags_IsPropertyContainer()
    {
        var component = new TestComponent();
        var key = new PropertyKey<int>("TestKey", typeof(ComponentBaseTests));

        component.Tags.Set(key, 42);

        Assert.Equal(42, component.Tags.Get(key));
    }

    [Fact]
    public void Collector_CanAddDisposables()
    {
        var component = new TestComponent();
        var holder = (ICollectorHolder)component;
        var disposable = new TestDisposable();

        holder.Collector.Add(disposable);
        // Just verify it doesn't throw
    }

    [Fact]
    public void Dispose_DisposesCollector()
    {
        var component = new TestComponent();
        var holder = (ICollectorHolder)component;
        var disposable = new TestDisposable();
        holder.Collector.Add(disposable);

        component.Dispose();

        Assert.True(component.IsDisposed);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeBy_AddsToCollector()
    {
        var component = new TestComponent();
        var disposable = new TestDisposable();

        var result = disposable.DisposeBy(component);

        Assert.Same(disposable, result);
    }

    [Fact]
    public void DisposeBy_DisposesWhenContainerDisposed()
    {
        var component = new TestComponent();
        var disposable = new TestDisposable();
        disposable.DisposeBy(component);

        component.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    private class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
