// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.Quantum.Tests.Helpers;
using Stride.Core.Quantum;

namespace Stride.Core.Presentation.Quantum.Tests;

/// <summary>
/// Collections whose items are themselves collections (a <see cref="Dictionary{TKey,TValue}"/> or a <see cref="List{T}"/>
/// holding lists): changing an inner collection must refresh the presenter of the item holding it, and removing an
/// item from the outer collection must leave the presenter of the removed item readable without throwing.
/// </summary>
public class TestNestedCollections
{
    private const string Key = "First";

    private static (TestInstanceContext Context, INodePresenter Root) BuildPresenters(Types.ClassWithNestedDictionary instance)
    {
        var context = new TestContainerContext().CreateInstanceContext(instance);
        var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
        return (context, root);
    }

    private static IObjectNode GetDictionaryNode(TestInstanceContext context)
        => context.RootNode[nameof(Types.ClassWithNestedDictionary.Dictionary)].Target!;

    private static IObjectNode GetNestedNode(TestInstanceContext context, string key)
        => GetDictionaryNode(context).IndexedTarget(new NodeIndex(key))!;

    [Fact]
    public void AddingAnItemToANestedListUpdatesTheParentItemPresenter()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a", "b"] } };
        var (context, root) = BuildPresenters(instance);
        var itemPresenter = root[nameof(Types.ClassWithNestedDictionary.Dictionary)].Children.Single(p => p.Name == Key);
        Assert.Equal(2, itemPresenter.Children.Count);

        GetNestedNode(context, Key).Add("c", new NodeIndex(2));

        Assert.Same(instance.Dictionary[Key], itemPresenter.Value);
        Assert.Equal(3, itemPresenter.Children.Count);
        Assert.Equal("a", itemPresenter.Children[0].Value);
        Assert.Equal("b", itemPresenter.Children[1].Value);
        Assert.Equal("c", itemPresenter.Children[2].Value);
    }

    [Fact]
    public void RemovingAnItemFromANestedListUpdatesTheParentItemPresenter()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a", "b"] } };
        var (context, root) = BuildPresenters(instance);
        var itemPresenter = root[nameof(Types.ClassWithNestedDictionary.Dictionary)].Children.Single(p => p.Name == Key);
        Assert.Equal(2, itemPresenter.Children.Count);

        GetNestedNode(context, Key).Remove("a", new NodeIndex(0));

        Assert.Same(instance.Dictionary[Key], itemPresenter.Value);
        Assert.Single(itemPresenter.Children);
        Assert.Equal("b", itemPresenter.Children[0].Value);
    }

    [Fact]
    public void UpdatingAnItemInANestedListUpdatesTheParentItemPresenter()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a", "b"] } };
        var (context, root) = BuildPresenters(instance);
        var itemPresenter = root[nameof(Types.ClassWithNestedDictionary.Dictionary)].Children.Single(p => p.Name == Key);

        GetNestedNode(context, Key).Update("x", new NodeIndex(0));

        Assert.Same(instance.Dictionary[Key], itemPresenter.Value);
        Assert.Equal(2, itemPresenter.Children.Count);
        Assert.Equal("x", itemPresenter.Children[0].Value);
        Assert.Equal("b", itemPresenter.Children[1].Value);
    }

    [Fact]
    public void AddingAKeyToTheDictionaryUpdatesTheCollectionPresenter()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a"] } };
        var (context, root) = BuildPresenters(instance);
        var dictionaryPresenter = root[nameof(Types.ClassWithNestedDictionary.Dictionary)];
        var dictionaryNode = GetDictionaryNode(context);

        dictionaryNode.Add(new List<string> { "new" }, new NodeIndex("Second"));

        var secondPresenter = dictionaryPresenter.Children.Single(p => p.Name == "Second");
        Assert.Same(instance.Dictionary["Second"], secondPresenter.Value);
        Assert.Single(secondPresenter.Children);
        Assert.Equal("new", secondPresenter.Children[0].Value);

        // The nested collection of the new key is tracked from the start
        dictionaryNode.IndexedTarget(new NodeIndex("Second"))!.Add("other", new NodeIndex(1));
        Assert.Equal(2, secondPresenter.Children.Count);
        Assert.Equal("other", secondPresenter.Children[1].Value);
    }

    [Fact]
    public void RemovingAKeyFromTheDictionaryLeavesTheRemovedItemPresenterReadable()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a", "b"] } };
        var (context, root) = BuildPresenters(instance);
        var dictionaryPresenter = root[nameof(Types.ClassWithNestedDictionary.Dictionary)];
        var itemPresenter = dictionaryPresenter.Children.Single(p => p.Name == Key);

        GetDictionaryNode(context).Remove(new List<string> { "a", "b" }, new NodeIndex(Key));

        Assert.Null(dictionaryPresenter.TryGetChild(Key));
        // The key no longer resolves: the presenter of the removed item is disposed, but reading it
        // (a view model binding or WPF automation may still do so while the removal is propagating) must not throw
        Assert.Null(itemPresenter.Value);
        itemPresenter.Dispose();
        Assert.Null(itemPresenter.Value);
    }

    [Fact]
    public void ReplacingAValueInTheDictionaryFollowsTheNewCollection()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a"] } };
        var (context, root) = BuildPresenters(instance);
        var dictionaryPresenter = root[nameof(Types.ClassWithNestedDictionary.Dictionary)];
        var dictionaryNode = GetDictionaryNode(context);
        var newList = new List<string> { "x" };

        dictionaryNode.Update(newList, new NodeIndex(Key));

        var itemPresenter = dictionaryPresenter.Children.Single(p => p.Name == Key);
        Assert.Same(newList, itemPresenter.Value);
        Assert.Single(itemPresenter.Children);
        Assert.Equal("x", itemPresenter.Children[0].Value);

        // Changes of the new list are tracked after the replacement
        GetNestedNode(context, Key).Add("y", new NodeIndex(1));
        Assert.Equal(2, itemPresenter.Children.Count);
        Assert.Equal("y", itemPresenter.Children[1].Value);
    }

    [Fact]
    public void AddingAnItemToANestedListInsideAListUpdatesTheParentItemPresenter()
    {
        var instance = new Types.ClassWithNestedList { String = "aaa", List = { new List<string> { "a", "b" }, new List<string> { "c" } } };
        var context = new TestContainerContext().CreateInstanceContext(instance);
        var root = context.Factory.CreateNodeHierarchy(context.RootNode, new GraphNodePath(context.RootNode));
        var listPresenter = root[nameof(Types.ClassWithNestedList.List)];
        var itemPresenter = listPresenter.Children[0];
        var otherItemPresenter = listPresenter.Children[1];

        context.RootNode[nameof(Types.ClassWithNestedList.List)].Target!.IndexedTarget(new NodeIndex(0))!.Add("d", new NodeIndex(2));

        Assert.Same(instance.List[0], itemPresenter.Value);
        Assert.Equal(3, itemPresenter.Children.Count);
        Assert.Equal("d", itemPresenter.Children[2].Value);

        // The sibling item is not touched by the nested change
        Assert.Same(otherItemPresenter, listPresenter.Children[1]);
        Assert.Single(otherItemPresenter.Children);
    }

    [Fact]
    public void AddingAnItemToANestedListUpdatesTheViewModel()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a"] } };
        var context = new TestContainerContext();
        var instanceContext = context.CreateInstanceContext(instance);
        var viewModel = instanceContext.CreateViewModel();
        var dictionaryNodeViewModel = viewModel.RootNode.GetChild(nameof(Types.ClassWithNestedDictionary.Dictionary))!;
        var itemNodeViewModel = dictionaryNodeViewModel.Children.Single();
        Assert.Single(itemNodeViewModel.Children);

        GetNestedNode(instanceContext, Key).Add("b", new NodeIndex(1));

        Assert.Equal(2, itemNodeViewModel.Children.Count);
        var addedChild = itemNodeViewModel.Children.Single(c => c.Name == "1");
        Assert.Equal("b", addedChild.NodeValue);
    }

    [Fact]
    public void RemovingAKeyFromTheDictionaryLeavesTheRemovedViewModelReadable()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a"] } };
        var context = new TestContainerContext();
        var instanceContext = context.CreateInstanceContext(instance);
        var viewModel = instanceContext.CreateViewModel();
        var dictionaryNodeViewModel = viewModel.RootNode.GetChild(nameof(Types.ClassWithNestedDictionary.Dictionary))!;
        var itemNodeViewModel = dictionaryNodeViewModel.Children.Single();

        GetDictionaryNode(instanceContext).Remove(new List<string> { "a" }, new NodeIndex(Key));

        Assert.Empty(dictionaryNodeViewModel.Children);
        // The view model of the removed item is destroyed, but reading it must not throw:
        // the WPF bindings (and WPF automation) may still access it while the removal is propagating
        _ = itemNodeViewModel.ToString();
        Assert.Null(itemNodeViewModel.NodeValue);
        _ = itemNodeViewModel.DisplayPath;
    }

    [Fact]
    public void ReplacingAValueInTheDictionaryUpdatesTheViewModelAndFollowsTheNewCollection()
    {
        var instance = new Types.ClassWithNestedDictionary { String = "aaa", Dictionary = { [Key] = ["a"] } };
        var context = new TestContainerContext();
        var instanceContext = context.CreateInstanceContext(instance);
        var viewModel = instanceContext.CreateViewModel();
        var dictionaryNodeViewModel = viewModel.RootNode.GetChild(nameof(Types.ClassWithNestedDictionary.Dictionary))!;
        var newList = new List<string> { "x" };

        GetDictionaryNode(instanceContext).Update(newList, new NodeIndex(Key));

        var itemNodeViewModel = dictionaryNodeViewModel.Children.Single(c => c.Name == Key);
        Assert.Single(itemNodeViewModel.Children);
        Assert.Equal("x", itemNodeViewModel.Children.Single().NodeValue);

        // Changes of the new list are tracked by the refreshed view model
        GetNestedNode(instanceContext, Key).Add("y", new NodeIndex(1));
        Assert.Equal(2, itemNodeViewModel.Children.Count);
        Assert.Equal("y", itemNodeViewModel.Children.Single(c => c.Name == "1").NodeValue);
    }
}
