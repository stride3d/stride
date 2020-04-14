// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Stride.Core.Assets.Quantum.Internal;
using Stride.Core.Assets.Quantum.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Quantum.Tests
{
    public class TestAssetCompositeHierarchyCloning
    {
        [Fact]
        public void TestSimpleCloneSubHierarchy()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.Empty(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
        }

        [Fact]
        public void TestCloneSubHierarchyWithInternalReference()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReference = x.Parts[GuidGenerator.Get(6)].Part);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.Empty(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part.Children[1], cloneRoot.Part.Children[0].MyReference);
        }

        [Fact]
        public void TestCloneSubHierarchyWithExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReferences = new List<Types.MyPart> { x.Parts[GuidGenerator.Get(2)].Part });
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.Empty(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(graph.Asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part, cloneRoot.Part.Children[0].MyReferences[0]);
        }

        [Fact]
        public void TestCloneSubHierarchyWithCleanExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.CleanExternalReferences, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.Empty(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
        }

        [Fact]
        public void TestCloneSubHierarchyWithInternalReferenceWithCleanExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReference = x.Parts[GuidGenerator.Get(6)].Part);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.CleanExternalReferences, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.Empty(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part.Children[1], cloneRoot.Part.Children[0].MyReference);
        }

        [Fact]
        public void TestCloneSubHierarchyWithExternalReferencesWithCleanExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReferences = new List<Types.MyPart> { x.Parts[GuidGenerator.Get(2)].Part });
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.CleanExternalReferences, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.Empty(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.Null(cloneRoot.Part.Children[0].MyReferences[0]);
        }


        [Fact]
        public void TestCloneSubHierarchyWithGenerateNewIdsForIdentifiableObjects()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.NotNull(remapping);
            Assert.Equal(3, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                Assert.Contains(part.Part.Id, remapping.Values);
                var matchingId = remapping.Single(x => x.Value == part.Part.Id).Key;
                Assert.NotEqual(part.Part.Id, matchingId);
                var matchingPart = graph.Asset.Hierarchy.Parts[matchingId];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.NotEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.NotEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.Contains(originalRoot.Part.Id, remapping.Keys);
            Assert.Equal(remapping[originalRoot.Part.Id], cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.NotEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Contains(originalRoot.Part.Children[0].Id, remapping.Keys);
            Assert.Equal(remapping[originalRoot.Part.Children[0].Id], cloneRoot.Part.Children[0].Id);
            Assert.NotEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.Contains(originalRoot.Part.Children[1].Id, remapping.Keys);
            Assert.Equal(remapping[originalRoot.Part.Children[1].Id], cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
        }

        [Fact]
        public void TestCloneSubHierarchyInstanceWithoutRemoveOverrides()
        {
            var baseAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, null, x => x.Parts[x.RootParts.Single().Id].Part.Name = "BaseName");
            var container = baseAsset.Container.NodeContainer;
            var derivedAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, baseAsset.Container);
            var instances = baseAsset.Asset.CreatePartInstances();
            var derivedRootId = instances.RootParts.Single().Id;
            derivedAsset.Graph.AddPartToAsset(instances.Parts, instances.Parts[derivedRootId], derivedAsset.Asset.Hierarchy.RootParts.Single(), 1);
            derivedAsset.Graph.RefreshBase();
            var partToChange = (AssetObjectNode)container.GetNode(instances.Parts.Single(x => x.Value.Part.Name == "BaseName").Value.Part);
            partToChange[nameof(Types.MyPart.Name)].Update("Overridden");
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(derivedAsset.Asset));
            var originalRoot = derivedAsset.Asset.Hierarchy.RootParts.Single();
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(container, derivedAsset.Asset, originalRoot.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneAsset = AssetHierarchyHelper.BuildAssetContainer(0, 0, 0, baseAsset.Container);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            cloneAsset.Graph.AddPartToAsset(clone.Parts, cloneRoot, null, 0);
            cloneAsset.Graph.RefreshBase();
            Assert.Empty(remapping);
            Assert.Equal(4, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = derivedAsset.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            var clonedChangedPart = (AssetObjectNode)container.GetNode(clone.Parts.Single(x => x.Value.Part.Id == (Guid)partToChange[nameof(IIdentifiable.Id)].Retrieve()).Value.Part);
            Assert.True(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentOverridden());
            Assert.False(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentInherited());
            Assert.Equal("Overridden", clonedChangedPart[nameof(Types.MyPart.Name)].Retrieve());
            Assert.Equal(partToChange[nameof(Types.MyPart.Name)].BaseNode, clonedChangedPart[nameof(Types.MyPart.Name)].BaseNode);
        }

        [Fact]
        public void TestCloneSubHierarchyInstanceWithRemoveOverrides()
        {
            var baseAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, null, x => x.Parts[x.RootParts.Single().Id].Part.Name = "BaseName");
            var container = baseAsset.Container.NodeContainer;
            var derivedAsset = AssetHierarchyHelper.BuildAssetContainer(1, 2, 1, baseAsset.Container);
            var instances = baseAsset.Asset.CreatePartInstances();
            var derivedRootId = instances.RootParts.Single().Id;
            derivedAsset.Graph.AddPartToAsset(instances.Parts, instances.Parts[derivedRootId], derivedAsset.Asset.Hierarchy.RootParts.Single(), 1);
            derivedAsset.Graph.RefreshBase();
            var partToChange = (AssetObjectNode)container.GetNode(instances.Parts.Single(x => x.Value.Part.Name == "BaseName").Value.Part);
            partToChange[nameof(Types.MyPart.Name)].Update("Overridden");
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(derivedAsset.Asset));
            var originalRoot = derivedAsset.Asset.Hierarchy.RootParts.Single();
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(container, derivedAsset.Asset, originalRoot.Id.Yield(), SubHierarchyCloneFlags.RemoveOverrides, out remapping);
            var cloneAsset = AssetHierarchyHelper.BuildAssetContainer(0, 0, 0, baseAsset.Container);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            cloneAsset.Graph.AddPartToAsset(clone.Parts, cloneRoot, null, 0);
            cloneAsset.Graph.RefreshBase();
            Assert.Empty(remapping);
            Assert.Equal(4, clone.Parts.Count);
            Assert.Single(clone.RootParts);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.Contains(rootPart, clone.Parts.Values.Select(x => x.Part));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = derivedAsset.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.NotEqual(matchingPart, part);
                Assert.NotEqual(matchingPart.Part, part.Part);
                Assert.Equal(matchingPart.Part.Id, part.Part.Id);
                Assert.Equal(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.Equal(originalRoot.Id, cloneRoot.Part.Id);
            Assert.NotEqual(originalRoot.Children[0], cloneRoot.Part.Children[0]);
            Assert.NotEqual(originalRoot.Children[1], cloneRoot.Part.Children[1]);
            Assert.Equal(originalRoot.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Equal(originalRoot.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.NotEqual(originalRoot.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.NotEqual(originalRoot.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.Equal(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            var clonedChangedPart = (AssetObjectNode)container.GetNode(clone.Parts.Single(x => x.Value.Part.Id == (Guid)partToChange[nameof(IIdentifiable.Id)].Retrieve()).Value.Part);
            // Note: currently, using RemoveOverrides does not clear the base (it just clears the overrides), so we should expect to still have the base linked.
            // This behavior could be changed in the future
            Assert.False(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentOverridden());
            Assert.True(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentInherited());
            Assert.Equal("Overridden", clonedChangedPart[nameof(Types.MyPart.Name)].Retrieve());
            Assert.Equal(partToChange[nameof(Types.MyPart.Name)].BaseNode, clonedChangedPart[nameof(Types.MyPart.Name)].BaseNode);
        }
    }
}
