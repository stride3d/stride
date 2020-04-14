// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xunit;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel.CopyPasteProcessors;
using Xenko.Core.Assets.Quantum;
using Xenko.Core;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Engine;
using Xenko.GameStudio.Tests.Helpers;

namespace Xenko.GameStudio.Tests
{
    public sealed class TestCopyPasteProperties
    {
        private ICopyPasteService service;
        private AssetPropertyGraphContainer propertyGraphContainer;

        public TestCopyPasteProperties()
        {
            propertyGraphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            service = TestHelper.CreateCopyPasteService(propertyGraphContainer);
        }

        [Fact]
        public void TestCopyPasteNewComponent()
        {
            var source = new Entity(); source.Components.Add(new ModelComponent() { IsShadowCaster = false });
            var target = new Entity();
            var propertyGraph = CreateScene(source, target);
            var copiedText = Copy(propertyGraph, source.Get<ModelComponent>());
            PasteIntoEntity(propertyGraph, copiedText, typeof(ModelComponent), target, NodeIndex.Empty, false);
            Assert.Equal(2, target.Components.Count);
            Assert.True(target.Components[0] is TransformComponent);
            Assert.True(target.Components[1] is ModelComponent);
            Assert.False(target.Get<ModelComponent>().IsShadowCaster);
        }

        private void PasteIntoEntity(AssetPropertyGraph propertyGraph, string copiedText, Type componentType, Entity entity, NodeIndex index, bool replace)
        {
            if (index == NodeIndex.Empty)
                Paste(propertyGraph, copiedText, componentType, typeof(EntityComponent), x => x["Hierarchy"].Target["Parts"].Target.IndexedTarget(new NodeIndex(entity.Id))["Entity"].Target["Components"], index, replace);
            else
                Paste(propertyGraph, copiedText, componentType, typeof(EntityComponent), x => x["Hierarchy"].Target["Parts"].Target.IndexedTarget(new NodeIndex(entity.Id))["Entity"].Target["Components"].Target, index, replace);
        }

        private string Copy(AssetPropertyGraph propertyGraph, object assetValue)
        {
            var copiedText = service.CopyFromAsset(propertyGraph, propertyGraph.Id, assetValue, false);
            Assert.False(string.IsNullOrEmpty(copiedText));
            return copiedText;
        }

        private void Paste(AssetPropertyGraph propertyGraph, string copiedText, Type deserializedType, Type expectedType, Func<IObjectNode, IGraphNode> targetNodeResolver, NodeIndex index, bool replace)
        {
            var asset = propertyGraph.RootNode.Retrieve();
            Assert.True(service.CanPaste(copiedText, asset.GetType(), expectedType));
            var result = service.DeserializeCopiedData(copiedText, asset, expectedType);
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Equal(1, result.Items.Count);

            var item = result.Items[0];
            Assert.NotNull(item);
            Assert.NotNull(item.Data);
            Assert.Equal(deserializedType, item.Data.GetType());
            Assert.NotNull(item.Processor);

            var targetNode = targetNodeResolver(propertyGraph.RootNode);
            var nodeAccessor = new NodeAccessor(targetNode, index);
            var propertyContainer = new PropertyContainer { { AssetPropertyPasteProcessor.IsReplaceKey, replace } };
            item.Processor.Paste(item, propertyGraph, ref nodeAccessor, ref propertyContainer);
        }

        private AssetPropertyGraph CreateScene(params Entity[] entities)
        {
            var scene = new SceneAsset();
            foreach (var entity in entities)
            {
                scene.Hierarchy.RootParts.Add(entity);
                scene.Hierarchy.Parts.Add(new EntityDesign(entity));
            }
            var assetItem = new AssetItem("", scene);
            var propertyGraph = AssetQuantumRegistry.ConstructPropertyGraph(propertyGraphContainer, assetItem, null);
            propertyGraphContainer.RegisterGraph(propertyGraph);
            return propertyGraph;
        }
    }
}
