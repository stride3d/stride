// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Xenko.Core.Assets.Quantum.Internal;
using Xenko.Core.Assets.Quantum.Tests.Helpers;
using Xenko.Core.Assets.Tests.Helpers;
using Xenko.Core;
using Xenko.Core.Extensions;

namespace Xenko.Core.Assets.Quantum.Tests
{
    [TestFixture]
    public class TestAssetCompositeHierarchyCloning
    {
        [Test]
        public void TestSimpleCloneSubHierarchy()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsEmpty(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
        }

        [Test]
        public void TestCloneSubHierarchyWithInternalReference()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReference = x.Parts[GuidGenerator.Get(6)].Part);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsEmpty(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part.Children[1], cloneRoot.Part.Children[0].MyReference);
        }

        [Test]
        public void TestCloneSubHierarchyWithExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReferences = new List<Types.MyPart> { x.Parts[GuidGenerator.Get(2)].Part });
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.None, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsEmpty(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(graph.Asset.Hierarchy.Parts[GuidGenerator.Get(2)].Part, cloneRoot.Part.Children[0].MyReferences[0]);
        }

        [Test]
        public void TestCloneSubHierarchyWithCleanExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.CleanExternalReferences, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsEmpty(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
        }

        [Test]
        public void TestCloneSubHierarchyWithInternalReferenceWithCleanExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReference = x.Parts[GuidGenerator.Get(6)].Part);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.CleanExternalReferences, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsEmpty(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part.Children[1], cloneRoot.Part.Children[0].MyReference);
        }

        [Test]
        public void TestCloneSubHierarchyWithExternalReferencesWithCleanExternalReferences()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2, x => x.Parts[GuidGenerator.Get(5)].Part.MyReferences = new List<Types.MyPart> { x.Parts[GuidGenerator.Get(2)].Part });
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.CleanExternalReferences, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsEmpty(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = graph.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(null, cloneRoot.Part.Children[0].MyReferences[0]);
        }


        [Test]
        public void TestCloneSubHierarchyWithGenerateNewIdsForIdentifiableObjects()
        {
            var graph = AssetHierarchyHelper.BuildAssetAndGraph(2, 2, 2);
            Debug.Write(AssetHierarchyHelper.PrintHierarchy(graph.Asset));
            var originalRoot = graph.Asset.Hierarchy.Parts[graph.Asset.Hierarchy.RootParts[1].Id];
            Dictionary<Guid, Guid> remapping;
            var clone = AssetCompositeHierarchyPropertyGraph<Types.MyPartDesign, Types.MyPart>.CloneSubHierarchies(graph.Container.NodeContainer, graph.Asset, originalRoot.Part.Id.Yield(), SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects, out remapping);
            var cloneRoot = clone.Parts[clone.RootParts.Single().Id];
            Assert.IsNotNull(remapping);
            Assert.AreEqual(3, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                Assert.Contains(part.Part.Id, remapping.Values);
                var matchingId = remapping.Single(x => x.Value == part.Part.Id).Key;
                Assert.AreNotEqual(part.Part.Id, matchingId);
                var matchingPart = graph.Asset.Hierarchy.Parts[matchingId];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreNotEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreNotEqual(originalRoot.Part.Id, cloneRoot.Part.Id);
            Assert.Contains(originalRoot.Part.Id, remapping.Keys);
            Assert.AreEqual(remapping[originalRoot.Part.Id], cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Part.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.Contains(originalRoot.Part.Children[0].Id, remapping.Keys);
            Assert.AreEqual(remapping[originalRoot.Part.Children[0].Id], cloneRoot.Part.Children[0].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.Contains(originalRoot.Part.Children[1].Id, remapping.Keys);
            Assert.AreEqual(remapping[originalRoot.Part.Children[1].Id], cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Part.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Part.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
        }

        [Test]
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
            Assert.IsEmpty(remapping);
            Assert.AreEqual(4, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = derivedAsset.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            var clonedChangedPart = (AssetObjectNode)container.GetNode(clone.Parts.Single(x => x.Value.Part.Id == (Guid)partToChange[nameof(IIdentifiable.Id)].Retrieve()).Value.Part);
            Assert.True(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentOverridden());
            Assert.False(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentInherited());
            Assert.AreEqual("Overridden", clonedChangedPart[nameof(Types.MyPart.Name)].Retrieve());
            Assert.AreEqual(partToChange[nameof(Types.MyPart.Name)].BaseNode, clonedChangedPart[nameof(Types.MyPart.Name)].BaseNode);
        }

        [Test]
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
            Assert.IsEmpty(remapping);
            Assert.AreEqual(4, clone.Parts.Count);
            Assert.AreEqual(1, clone.RootParts.Count);
            foreach (var rootPart in clone.RootParts)
            {
                Assert.True(clone.Parts.Values.Select(x => x.Part).Contains(rootPart));
            }
            foreach (var part in clone.Parts.Values)
            {
                var matchingPart = derivedAsset.Asset.Hierarchy.Parts[part.Part.Id];
                Assert.AreNotEqual(matchingPart, part);
                Assert.AreNotEqual(matchingPart.Part, part.Part);
                Assert.AreEqual(matchingPart.Part.Id, part.Part.Id);
                Assert.AreEqual(matchingPart.Part.Name, part.Part.Name);
            }
            Assert.AreEqual(originalRoot.Id, cloneRoot.Part.Id);
            Assert.AreNotEqual(originalRoot.Children[0], cloneRoot.Part.Children[0]);
            Assert.AreNotEqual(originalRoot.Children[1], cloneRoot.Part.Children[1]);
            Assert.AreEqual(originalRoot.Children[0].Id, cloneRoot.Part.Children[0].Id);
            Assert.AreEqual(originalRoot.Children[1].Id, cloneRoot.Part.Children[1].Id);
            Assert.AreNotEqual(originalRoot.Children[0].Parent, cloneRoot.Part.Children[0].Parent);
            Assert.AreNotEqual(originalRoot.Children[1].Parent, cloneRoot.Part.Children[1].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[0].Parent);
            Assert.AreEqual(cloneRoot.Part, cloneRoot.Part.Children[1].Parent);
            var clonedChangedPart = (AssetObjectNode)container.GetNode(clone.Parts.Single(x => x.Value.Part.Id == (Guid)partToChange[nameof(IIdentifiable.Id)].Retrieve()).Value.Part);
            // Note: currently, using RemoveOverrides does not clear the base (it just clears the overrides), so we should expect to still have the base linked.
            // This behavior could be changed in the future
            Assert.False(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentOverridden());
            Assert.True(clonedChangedPart[nameof(Types.MyPart.Name)].IsContentInherited());
            Assert.AreEqual("Overridden", clonedChangedPart[nameof(Types.MyPart.Name)].Retrieve());
            Assert.AreEqual(partToChange[nameof(Types.MyPart.Name)].BaseNode, clonedChangedPart[nameof(Types.MyPart.Name)].BaseNode);
        }
    }
}
