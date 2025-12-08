using System.Collections.Specialized;
using Stride.Core.Collections;
using Xunit;

namespace Stride.Core.Tests.Collections;

public class TrackingDictionaryTests
{
    [Fact]
    public void Constructor_CreatesEmptyDictionary()
    {
        var dict = new TrackingDictionary<string, int>();

        Assert.Empty(dict);
    }

    [Fact]
    public void Add_AddsItemAndTriggersEvent()
    {
        var dict = new TrackingDictionary<string, int>();
        TrackingCollectionChangedEventArgs? eventArgs = null;
        dict.CollectionChanged += (sender, args) => eventArgs = args;

        dict.Add("key1", 10);

        Assert.Single(dict);
        Assert.NotNull(eventArgs);
        Assert.Equal(NotifyCollectionChangedAction.Add, eventArgs.Action);
        Assert.Equal("key1", eventArgs.Key);
        Assert.Equal(10, eventArgs.Item);
    }

    [Fact]
    public void Remove_RemovesItemAndTriggersEvent()
    {
        var dict = new TrackingDictionary<string, int> { { "key1", 10 } };
        TrackingCollectionChangedEventArgs? eventArgs = null;
        dict.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
                eventArgs = args;
        };

        var result = dict.Remove("key1");

        Assert.True(result);
        Assert.Empty(dict);
        Assert.NotNull(eventArgs);
        Assert.Equal(NotifyCollectionChangedAction.Remove, eventArgs.Action);
        Assert.Equal("key1", eventArgs.Key);
        Assert.Equal(10, eventArgs.Item);
    }

    [Fact]
    public void Remove_ReturnsFalseForNonExistingKey()
    {
        var dict = new TrackingDictionary<string, int>();

        var result = dict.Remove("nonexisting");

        Assert.False(result);
    }

    [Fact]
    public void Indexer_Set_UpdatesValueAndTriggersEvents()
    {
        var dict = new TrackingDictionary<string, int> { { "key1", 10 } };
        var eventCount = 0;
        dict.CollectionChanged += (sender, args) => eventCount++;

        dict["key1"] = 20;

        Assert.Equal(20, dict["key1"]);
        // Setting existing key triggers remove and add events
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void Indexer_Get_ReturnsValue()
    {
        var dict = new TrackingDictionary<string, int> { { "key1", 10 } };

        var value = dict["key1"];

        Assert.Equal(10, value);
    }

    [Fact]
    public void ContainsKey_ReturnsTrueForExistingKey()
    {
        var dict = new TrackingDictionary<string, int> { { "key1", 10 } };

        Assert.True(dict.ContainsKey("key1"));
        Assert.False(dict.ContainsKey("key2"));
    }

    [Fact]
    public void TryGetValue_ReturnsValueForExistingKey()
    {
        var dict = new TrackingDictionary<string, int> { { "key1", 10 } };

        var result = dict.TryGetValue("key1", out var value);

        Assert.True(result);
        Assert.Equal(10, value);
    }

    [Fact]
    public void TryGetValue_ReturnsFalseForNonExistingKey()
    {
        var dict = new TrackingDictionary<string, int>();

        var result = dict.TryGetValue("key1", out var value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        var dict = new TrackingDictionary<string, int>
        {
            { "key1", 10 },
            { "key2", 20 },
            { "key3", 30 }
        };

        var keys = dict.Keys;

        Assert.Equal(3, keys.Count);
        Assert.Contains("key1", keys);
        Assert.Contains("key2", keys);
        Assert.Contains("key3", keys);
    }

    [Fact]
    public void Values_ReturnsAllValues()
    {
        var dict = new TrackingDictionary<string, int>
        {
            { "key1", 10 },
            { "key2", 20 },
            { "key3", 30 }
        };

        var values = dict.Values;

        Assert.Equal(3, values.Count);
        Assert.Contains(10, values);
        Assert.Contains(20, values);
        Assert.Contains(30, values);
    }

    [Fact]
    public void Clear_RemovesAllItemsAndTriggersEvents()
    {
        var dict = new TrackingDictionary<string, int>
        {
            { "key1", 10 },
            { "key2", 20 }
        };
        var removeCount = 0;
        dict.CollectionChanged += (sender, args) =>
        {
            if (args.Action == NotifyCollectionChangedAction.Remove)
                removeCount++;
        };

        dict.Clear();

        Assert.Empty(dict);
        Assert.Equal(2, removeCount);
    }

    [Fact]
    public void Enumerator_IteratesThroughAllKeyValuePairs()
    {
        var dict = new TrackingDictionary<string, int>
        {
            { "key1", 10 },
            { "key2", 20 },
            { "key3", 30 }
        };

        var pairs = new Dictionary<string, int>();
        foreach (var kvp in dict)
        {
            pairs.Add(kvp.Key, kvp.Value);
        }

        Assert.Equal(3, pairs.Count);
        Assert.Equal(10, pairs["key1"]);
        Assert.Equal(20, pairs["key2"]);
        Assert.Equal(30, pairs["key3"]);
    }

    [Fact]
    public void CollectionChanged_EventHandlerOrder_FiresInRegistrationOrder()
    {
        var dict = new TrackingDictionary<string, int>();
        var events = new List<string>();

        // Add two handlers
        dict.CollectionChanged += (s, e) => events.Add("Handler1");
        dict.CollectionChanged += (s, e) => events.Add("Handler2");

        dict.Add("key1", 10);

        // Add events should fire in the order they were registered
        Assert.Equal(2, events.Count);
        Assert.Equal("Handler1", events[0]);
        Assert.Equal("Handler2", events[1]);
    }

    [Fact]
    public void CollectionChanged_CanBeUnsubscribed()
    {
        var dict = new TrackingDictionary<string, int>();
        var eventCount = 0;
        EventHandler<TrackingCollectionChangedEventArgs> handler = (s, e) => eventCount++;

        dict.CollectionChanged += handler;
        dict.Add("key1", 10);
        Assert.Equal(1, eventCount);

        dict.CollectionChanged -= handler;
        dict.Add("key2", 20);
        Assert.Equal(1, eventCount); // Should not increase
    }
}
