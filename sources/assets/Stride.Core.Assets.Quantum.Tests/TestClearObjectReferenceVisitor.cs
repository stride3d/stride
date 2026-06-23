// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="ClearObjectReferenceVisitor"/>. When an identifiable object is removed, any object
/// reference still pointing at it must be cleared (set to null) so the asset doesn't dangle. The visitor clears
/// references to a given set of ids, optionally filtered by a predicate.
/// </summary>
public class TestClearObjectReferenceVisitor
{
    private static AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph> BuildWithReference(Types.MyReferenceable target)
    {
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2 { Reference = target });
        container.BuildGraph();
        return container;
    }

    [Fact]
    public void TestClearsReferenceToTargetId()
    {
        var target = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "target" };
        var container = BuildWithReference(target);
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));
        Assert.Same(target, container.Graph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Retrieve());

        var visitor = new ClearObjectReferenceVisitor(definition, [target.Id]);
        visitor.Visit(container.Graph.RootNode);

        Assert.Null(container.Graph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Retrieve());
    }

    [Fact]
    public void TestDoesNotClearWhenPredicateReturnsFalse()
    {
        var target = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "target" };
        var container = BuildWithReference(target);
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        var visitor = new ClearObjectReferenceVisitor(definition, [target.Id], (node, index) => false);
        visitor.Visit(container.Graph.RootNode);

        Assert.Same(target, container.Graph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Retrieve());
    }

    [Fact]
    public void TestDoesNotClearReferenceToOtherIds()
    {
        var target = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "target" };
        var container = BuildWithReference(target);
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        // Clearing an unrelated id leaves the reference intact.
        var visitor = new ClearObjectReferenceVisitor(definition, [GuidGenerator.Get(99)]);
        visitor.Visit(container.Graph.RootNode);

        Assert.Same(target, container.Graph.RootNode[nameof(Types.MyAssetWithRef2.Reference)].Retrieve());
    }

    [Fact]
    public void TestConstructorValidatesArguments()
    {
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));
        Assert.Throws<ArgumentNullException>(() => new ClearObjectReferenceVisitor(null!, [GuidGenerator.Get(1)]));
        Assert.Throws<ArgumentNullException>(() => new ClearObjectReferenceVisitor(definition, null!));
    }
}
