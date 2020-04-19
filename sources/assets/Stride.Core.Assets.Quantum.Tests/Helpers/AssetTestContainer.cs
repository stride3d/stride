// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Xunit;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Quantum.Tests.Helpers
{
    public class AssetTestContainer
    {
        public AssetTestContainer(AssetPropertyGraphContainer container, Asset asset)
        {
            Container = container;
            AssetItem = new AssetItem("MyAsset", asset);
        }

        public AssetPropertyGraphContainer Container { get; }

        public AssetItem AssetItem { get; }


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
    }

    public class AssetTestContainer<TAsset, TAssetPropertyGraph> : AssetTestContainer where TAsset : Asset where TAssetPropertyGraph : AssetPropertyGraph
    {
        private readonly LoggerResult logger = new LoggerResult();

        public AssetTestContainer(AssetPropertyGraphContainer container, TAsset asset)
            : base(container, asset)
        {
        }

        public AssetTestContainer(TAsset asset)
            : base(new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } }), asset)
        {
        }

        public TAsset Asset => (TAsset)AssetItem.Asset;

        public TAssetPropertyGraph Graph { get; private set; }

        public void BuildGraph()
        {
            var baseGraph = AssetQuantumRegistry.ConstructPropertyGraph(Container, AssetItem, logger);
            Container.RegisterGraph(baseGraph);
            Assert.True(baseGraph is TAssetPropertyGraph);
            Graph = (TAssetPropertyGraph)baseGraph;
        }

        public AssetTestContainer<TAsset, TAssetPropertyGraph> DeriveAsset()
        {
            var derivedAsset = (TAsset)Asset.CreateDerivedAsset("MyAsset");
            var result = new AssetTestContainer<TAsset, TAssetPropertyGraph>(Container, derivedAsset);
            result.BuildGraph();
            return result;
        }

        public static AssetTestContainer<TAsset, TAssetPropertyGraph> LoadFromYaml(string yaml)
        {
            var asset = AssetFileSerializer.Load<TAsset>(ToStream(yaml), $"MyAsset{Types.FileExtension}");
            var graphContainer = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var assetContainer = new AssetTestContainer<TAsset, TAssetPropertyGraph>(graphContainer, asset.Asset);
            asset.YamlMetadata.CopyInto(assetContainer.AssetItem.YamlMetadata);
            assetContainer.BuildGraph();
            return assetContainer;
        }
    }
}
