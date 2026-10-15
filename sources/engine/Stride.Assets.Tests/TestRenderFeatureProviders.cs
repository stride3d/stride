// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Rendering;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Yaml;
using Stride.Core.Diagnostics;
using Stride.Core.Quantum;
using Stride.Rendering;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Render features added to a compositor derived from an engine one are its own items (override marks), which
    /// reconciling with the base keeps.
    /// </summary>
    public class TestRenderFeatureProviders
    {
        [Fact]
        public void DerivedCompositorKeepsTheRenderFeaturesItAdds()
        {
            var (derivedItem, graph) = DeriveCompositor(derived => RenderFeatureProviders.AddRenderFeatures(derived.Asset, derived.YamlMetadata, [new MeshRenderFeature()]));

            var compositor = (GraphicsCompositorAsset)derivedItem.Asset;
            Assert.IsType<MeshRenderFeature>(Assert.Single(compositor.RenderFeatures));
            var renderFeatures = (IAssetObjectNode)graph.RootNode[nameof(GraphicsCompositorAsset.RenderFeatures)].Target;
            Assert.True(renderFeatures.IsItemOverridden(new NodeIndex(0)));
        }

        [Fact]
        public void RenderFeatureAddedWithoutOwningItIsDropped()
        {
            // The item has its id but its override mark is lost (metadata the asset item does not get)
            var (derivedItem, _) = DeriveCompositor(derived => RenderFeatureProviders.AddRenderFeatures(derived.Asset, new AttachedYamlAssetMetadata(), [new MeshRenderFeature()]));

            Assert.Empty(((GraphicsCompositorAsset)derivedItem.Asset).RenderFeatures);
        }

        // A base compositor with Opaque and Transparent stages and no render feature, and one derived from it: the change
        // is made to the derived one before its property graph exists, then the graph reconciles it with the base
        private static (AssetItem DerivedItem, AssetPropertyGraph Graph) DeriveCompositor(Action<(GraphicsCompositorAsset Asset, AttachedYamlAssetMetadata YamlMetadata)> change)
        {
            var logger = new LoggerResult();
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });

            var baseCompositor = new GraphicsCompositorAsset();
            baseCompositor.RenderStages.Add(new RenderStage("Opaque", "Main"));
            baseCompositor.RenderStages.Add(new RenderStage("Transparent", "Main"));
            var baseItem = new AssetItem("BaseCompositor", baseCompositor);
            container.RegisterGraph(AssetQuantumRegistry.ConstructPropertyGraph(container, baseItem, logger));

            var derived = (GraphicsCompositorAsset)baseCompositor.CreateDerivedAsset("BaseCompositor");
            var derivedItem = new AssetItem("DerivedCompositor", derived);
            change((derived, derivedItem.YamlMetadata));
            var graph = AssetQuantumRegistry.ConstructPropertyGraph(container, derivedItem, logger);
            container.RegisterGraph(graph);
            graph.Initialize();

            Assert.False(logger.HasErrors, logger.ToText());
            return (derivedItem, graph);
        }
    }
}
