// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="Visitors.ObjectReferencePathGenerator"/> (exercised through
/// <see cref="AssetPropertyGraph.GenerateObjectReferencesForSerialization"/>). On save, every object reference
/// is emitted as the target object's id so the reference can be re-resolved on load. Owned (non-reference)
/// members must not be emitted.
/// </summary>
public class TestObjectReferencePathGenerator
{
    [Fact]
    public void TestObjectReferenceIsEmittedAsTargetId()
    {
        var referenced = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "referenced" };
        var owned = new Types.MyReferenceable { Id = GuidGenerator.Get(1), Value = "owned" };
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2 { NonReference = owned, Reference = referenced });
        container.BuildGraph();

        var references = container.Graph.GenerateObjectReferencesForSerialization(container.Graph.RootNode);

        // The reference target is emitted, the owned object is not.
        Assert.Contains(references, kv => kv.Value == referenced.Id);
        Assert.DoesNotContain(references, kv => kv.Value == owned.Id);
    }

    [Fact]
    public void TestShouldOutputReferenceIsNotEmittedWhenNoReferences()
    {
        var owned = new Types.MyReferenceable { Id = GuidGenerator.Get(1), Value = "owned" };
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2 { NonReference = owned });
        container.BuildGraph();

        var references = container.Graph.GenerateObjectReferencesForSerialization(container.Graph.RootNode);

        Assert.Empty(references);
    }
}
