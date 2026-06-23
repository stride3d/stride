// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Assets.Tests.Helpers;
using Xunit;

namespace Stride.Core.Assets.Quantum.Tests;

/// <summary>
/// Unit tests for <see cref="ExternalReferenceCollector"/>. When cloning part of an asset, references that point
/// at objects <em>outside</em> the cloned sub-graph (external references) must be treated differently from
/// references to objects owned <em>inside</em> it. The collector classifies each referenced identifiable
/// accordingly; an object that is both owned and referenced is considered internal.
/// </summary>
public class TestExternalReferenceCollector
{
    [Fact]
    public void TestReferenceToNonOwnedObjectIsExternal()
    {
        // The referenced object is not owned anywhere in the graph -> external.
        var external = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "external" };
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2 { Reference = external });
        container.BuildGraph();
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        var external1 = ExternalReferenceCollector.GetExternalReferences(definition, container.Graph.RootNode);

        Assert.Contains(external, external1);
    }

    [Fact]
    public void TestReferenceToOwnedObjectIsNotExternal()
    {
        // The same object is both owned (NonReference) and referenced (Reference) -> internal, not external.
        var shared = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "shared" };
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2 { NonReference = shared, Reference = shared });
        container.BuildGraph();
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        var external = ExternalReferenceCollector.GetExternalReferences(definition, container.Graph.RootNode);

        Assert.DoesNotContain(shared, external);
    }

    [Fact]
    public void TestExternalReferenceAccessorsAreRecorded()
    {
        var external = new Types.MyReferenceable { Id = GuidGenerator.Get(2), Value = "external" };
        var container = new AssetTestContainer<Types.MyAssetWithRef2, Types.MyAssetBasePropertyGraph>(new Types.MyAssetWithRef2 { Reference = external });
        container.BuildGraph();
        var definition = AssetQuantumRegistry.GetDefinition(typeof(Types.MyAssetWithRef2));

        var accessors = ExternalReferenceCollector.GetExternalReferenceAccessors(definition, container.Graph.RootNode);

        Assert.True(accessors.ContainsKey(external));
        Assert.NotEmpty(accessors[external]);
    }
}
