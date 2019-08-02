// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Tests.Helpers;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.Quantum;
using Xenko.Assets.Presentation.ViewModel.CopyPasteProcessors;
using Xenko.Engine;
using Xenko.GameStudio.Tests.Helpers;

namespace Xenko.GameStudio.Tests
{
    public class TestCopyPasteWithEntities
    {
        public class CopyPasteTest
        {
            protected CopyPasteTest([NotNull] Asset asset)
            {
                Container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
                AssetItem = new AssetItem("MyAsset", asset);
                var assetGraph = AssetQuantumRegistry.ConstructPropertyGraph(Container, AssetItem, null);
                Assert.IsAssignableFrom<EntityHierarchyPropertyGraph>(assetGraph);
                AssetGraph = (EntityHierarchyPropertyGraph)assetGraph;
            }

            [NotNull]
            public static Stream ToStream(string str)
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(str);
                writer.Flush();
                stream.Position = 0;
                return stream;
            }

            public AssetPropertyGraphContainer Container { get; }
            public AssetItem AssetItem { get; }
            public EntityHierarchyPropertyGraph AssetGraph { get; }
        }

        public class CopyPasteTest<T> : CopyPasteTest where T : Asset
        {
            public CopyPasteTest([NotNull] T asset)
                : base(asset)
            {
            }

            [NotNull]
            public T Asset => (T)AssetItem.Asset;
        }

        public class ReferencingComponent : EntityComponent
        {
            public Entity MyEntity { get; set; }
            public ReferencingComponent MyComponent { get; set; }
        }

        [Fact]
        public void TestCopyPasteEntityAtRoot()
        {
            var sceneAsset = new SceneAsset();
            var entity = new EntityDesign { Entity = new Entity { Id = GuidGenerator.Get(1) } };
            entity.Entity.Transform.Position = Vector3.UnitZ;
            sceneAsset.Hierarchy.RootParts.Add(entity.Entity);
            sceneAsset.Hierarchy.Parts.Add(entity);

            var assetTest = new CopyPasteTest<SceneAsset>(sceneAsset);
            var service = TestHelper.CreateCopyPasteService();
            service.PropertyGraphContainer.RegisterGraph(assetTest.AssetGraph);
            var clipboard = Copy(service, assetTest.AssetGraph, new[] { entity });
            Paste(service, clipboard, assetTest.AssetGraph, null, null);
            Assert.Equal(2, assetTest.Asset.Hierarchy.Parts.Count);
            Assert.True(assetTest.Asset.Hierarchy.Parts.Values.Contains(entity));

            var pastedEntity = assetTest.Asset.Hierarchy.Parts.Values.Single(x => x != entity);
            Assert.Equal(2, assetTest.Asset.Hierarchy.RootParts.Count);
            Assert.Contains(pastedEntity.Entity, assetTest.Asset.Hierarchy.RootParts);
            Assert.Equal(string.Empty, pastedEntity.Folder);
            Assert.NotEqual(entity.Entity.Id, pastedEntity.Entity.Id);
            Assert.NotEqual(entity.Entity.Transform.Id, pastedEntity.Entity.Transform.Id);
            Assert.Equal(Vector3.UnitZ, pastedEntity.Entity.Transform.Position);
        }

        [Fact]
        public void TestCopyPasteEntityAsChild()
        {
            var sceneAsset = new SceneAsset();
            var entity = new EntityDesign { Entity = new Entity { Id = GuidGenerator.Get(1) } };
            entity.Entity.Transform.Position = Vector3.UnitZ;
            sceneAsset.Hierarchy.RootParts.Add(entity.Entity);
            sceneAsset.Hierarchy.Parts.Add(entity);

            var assetTest = new CopyPasteTest<SceneAsset>(sceneAsset);
            var service = TestHelper.CreateCopyPasteService();
            service.PropertyGraphContainer.RegisterGraph(assetTest.AssetGraph);
            var clipboard = Copy(service, assetTest.AssetGraph, new[] { entity });
            Paste(service, clipboard, assetTest.AssetGraph, entity, null);
            Assert.Equal(2, assetTest.Asset.Hierarchy.Parts.Count);
            Assert.True(assetTest.Asset.Hierarchy.Parts.Values.Contains(entity));

            var pastedEntity = assetTest.Asset.Hierarchy.Parts.Values.Single(x => x != entity);
            Assert.Single(assetTest.Asset.Hierarchy.RootParts);
            Assert.Contains(entity.Entity, assetTest.Asset.Hierarchy.RootParts);
            Assert.Equal(string.Empty, pastedEntity.Folder);
            Assert.NotEqual(entity.Entity.Id, pastedEntity.Entity.Id);
            Assert.NotEqual(entity.Entity.Transform.Id, pastedEntity.Entity.Transform.Id);
            Assert.Equal(entity.Entity.Transform, pastedEntity.Entity.Transform.Parent);
            Assert.Contains(pastedEntity.Entity.Transform, entity.Entity.Transform.Children);
            Assert.Equal(Vector3.UnitZ, pastedEntity.Entity.Transform.Position);
        }

        [CanBeNull]
        private static string Copy([NotNull] ICopyPasteService service, [NotNull] EntityHierarchyPropertyGraph assetGraph, [NotNull] IEnumerable<EntityDesign> commonRoots)
        {
            // copy selected hierarchy
            var hierarchy = EntityHierarchyPropertyGraph.CloneSubHierarchies(assetGraph.Container.NodeContainer, assetGraph.Asset, commonRoots.Select(r => r.Entity.Id), SubHierarchyCloneFlags.None, out _);
            // visitor on this temporary asset
            var text = service.CopyFromAsset(assetGraph, assetGraph.Id, hierarchy, false);
            return text;
        }

        private static void Paste([NotNull] ICopyPasteService service, string text, [NotNull] EntityHierarchyPropertyGraph assetGraph, EntityDesign target, string folderName)
        {
            var data = service.DeserializeCopiedData(text, assetGraph.Asset, typeof(Entity));
            Assert.NotNull(data);
            Assert.NotNull(data.Items);
            Assert.Equal(1, data.Items.Count);

            var item = data.Items[0];
            Assert.NotNull(item);
            Assert.NotNull(item.Data);
            Assert.NotNull(item.Processor);

            var targetNode = target != null ? assetGraph.Container.NodeContainer.GetNode(target.Entity) : assetGraph.RootNode;
            var nodeAccessor = new NodeAccessor(targetNode, NodeIndex.Empty);
            var propertyContainer = new PropertyContainer { { EntityHierarchyPasteProcessor.TargetFolderKey, folderName } };
            item.Processor.Paste(item, assetGraph, ref nodeAccessor, ref propertyContainer);
        }
    }
}
