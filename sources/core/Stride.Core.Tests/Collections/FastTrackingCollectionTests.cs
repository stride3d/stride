// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class FastTrackingCollectionTests
{
    [Fact]
    public void Constructor_CreatesEmptyCollection()
    {
        var collection = new FastTrackingCollection<int>();
        Assert.Empty(collection);
    }

    [Fact]
    public void Add_RaisesCollectionChangedEvent()
    {
        var collection = new FastTrackingCollection<int>();
        var eventRaised = false;
        FastTrackingCollectionChangedEventArgs capturedArgs = default;

        FastTrackingCollection<int>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) =>
        {
            eventRaised = true;
            capturedArgs = e;
        };
        collection.CollectionChanged += handler;

        collection.Add(42);

        Assert.True(eventRaised);
        Assert.Equal(NotifyCollectionChangedAction.Add, capturedArgs.Action);
        Assert.Equal(42, capturedArgs.Item);
        Assert.Equal(0, capturedArgs.Index);
    }

    [Fact]
    public void Remove_RaisesCollectionChangedEvent()
    {
        var collection = new FastTrackingCollection<string> { "test" };
        var eventRaised = false;
        FastTrackingCollectionChangedEventArgs capturedArgs = default;

        FastTrackingCollection<string>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) =>
        {
            eventRaised = true;
            capturedArgs = e;
        };
        collection.CollectionChanged += handler;

        collection.RemoveAt(0);

        Assert.True(eventRaised);
        Assert.Equal(NotifyCollectionChangedAction.Remove, capturedArgs.Action);
        Assert.Equal("test", capturedArgs.Item);
        Assert.Equal(0, capturedArgs.Index);
    }

    [Fact]
    public void Clear_RaisesRemoveEventsForAllItems()
    {
        var collection = new FastTrackingCollection<int> { 1, 2, 3 };
        var removeCount = 0;
        var removedItems = new List<int>();

        FastTrackingCollection<int>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                removeCount++;
                removedItems.Add((int)e.Item!);
            }
        };
        collection.CollectionChanged += handler;

        collection.Clear();

        Assert.Equal(3, removeCount);
        Assert.Equal(new[] { 3, 2, 1 }, removedItems); // Reverse order
        Assert.Empty(collection);
    }

    [Fact]
    public void SetItem_RaisesRemoveAndAddEvents()
    {
        var collection = new FastTrackingCollection<string> { "old" };
        var events = new List<(NotifyCollectionChangedAction, object?)>();

        FastTrackingCollection<string>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) =>
        {
            events.Add((e.Action, e.Item));
        };
        collection.CollectionChanged += handler;

        collection[0] = "new";

        Assert.Equal(2, events.Count);
        Assert.Equal(NotifyCollectionChangedAction.Remove, events[0].Item1);
        Assert.Equal("old", events[0].Item2);
        Assert.Equal(NotifyCollectionChangedAction.Add, events[1].Item1);
        Assert.Equal("new", events[1].Item2);
    }

    [Fact]
    public void Insert_RaisesAddEventAtCorrectIndex()
    {
        var collection = new FastTrackingCollection<int> { 1, 3 };
        var eventRaised = false;
        FastTrackingCollectionChangedEventArgs capturedArgs = default;

        FastTrackingCollection<int>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) =>
        {
            eventRaised = true;
            capturedArgs = e;
        };
        collection.CollectionChanged += handler;

        collection.Insert(1, 2);

        Assert.True(eventRaised);
        Assert.Equal(NotifyCollectionChangedAction.Add, capturedArgs.Action);
        Assert.Equal(2, capturedArgs.Item);
        Assert.Equal(1, capturedArgs.Index);
        Assert.Equal(new[] { 1, 2, 3 }, collection);
    }

    [Fact]
    public void MultipleHandlers_AllReceiveEvents()
    {
        var collection = new FastTrackingCollection<int>();
        var handler1Called = false;
        var handler2Called = false;

        FastTrackingCollection<int>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler1 =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) => handler1Called = true;
        FastTrackingCollection<int>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler2 =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) => handler2Called = true;
        collection.CollectionChanged += handler1;
        collection.CollectionChanged += handler2;

        collection.Add(1);

        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public void RemoveHandler_StopsReceivingEvents()
    {
        var collection = new FastTrackingCollection<int>();
        var eventCount = 0;

        FastTrackingCollection<int>.FastEventHandler<FastTrackingCollectionChangedEventArgs> handler =
            (object sender, ref FastTrackingCollectionChangedEventArgs e) => eventCount++;

        collection.CollectionChanged += handler;
        collection.Add(1);
        Assert.Equal(1, eventCount);

        collection.CollectionChanged -= handler;
        collection.Add(2);
        Assert.Equal(1, eventCount); // Should not increment
    }

    [Fact]
    public void NoHandlers_OperationsWorkNormally()
    {
        var collection = new FastTrackingCollection<int>();

        collection.Add(1);
        collection.Add(2);
        collection.RemoveAt(0);
        collection.Clear();

        Assert.Empty(collection);
    }
}
