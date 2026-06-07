// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class TrackingCollectionTests
{
    [Fact]
    public void Remove_RaisesCollectionChangedAfterItemRemoved()
    {
        var collection = new TrackingCollection<int> { 1 };
        var itemRemovedBeforeEvent = false;

        collection.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
                itemRemovedBeforeEvent = !collection.Contains(1);
        };

        collection.RemoveAt(0);

        Assert.True(itemRemovedBeforeEvent);
    }

    [Fact]
    public void Clear_RaisesCollectionChangedAfterItemsRemoved()
    {
        var collection = new TrackingCollection<int> { 1, 2, 3 };
        var removedItemsWereGone = new List<bool>();

        collection.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
                removedItemsWereGone.Add(!collection.Contains((int)args.Item!));
        };

        collection.Clear();

        Assert.Equal(new[] { true, true, true }, removedItemsWereGone);
    }
}
