// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Assets.Quantum.Tests.Helpers
{
    public class DeriveAssetTest<TAsset, TAssetPropertyGraph> where TAsset : Asset where TAssetPropertyGraph : AssetPropertyGraph
    {
        private DeriveAssetTest(AssetTestContainer<TAsset, TAssetPropertyGraph> baseAsset, AssetTestContainer<TAsset, TAssetPropertyGraph> derivedAsset, AssetTestContainer<TAsset, TAssetPropertyGraph> subDerivedAsset)
        {
            Base = baseAsset;
            Derived = derivedAsset;
            SubDerived = subDerivedAsset;
        }

        public TAsset BaseAsset => (TAsset)BaseAssetItem.Asset;
        public TAsset DerivedAsset => (TAsset)DerivedAssetItem.Asset;
        public TAsset SubDerivedAsset => (TAsset)SubDerivedAssetItem.Asset;
        public AssetItem BaseAssetItem => Base.AssetItem;
        public AssetItem DerivedAssetItem => Derived.AssetItem;
        public AssetItem SubDerivedAssetItem => SubDerived.AssetItem;
        public TAssetPropertyGraph BaseGraph => Base.Graph;
        public TAssetPropertyGraph DerivedGraph => Derived.Graph;
        public TAssetPropertyGraph SubDerivedGraph => SubDerived.Graph;

        public AssetTestContainer<TAsset, TAssetPropertyGraph> Base { get; }
        public AssetTestContainer<TAsset, TAssetPropertyGraph> Derived { get; }
        public AssetTestContainer<TAsset, TAssetPropertyGraph> SubDerived { get; }

        public static DeriveAssetTest<TAsset, TAssetPropertyGraph> DeriveAsset(TAsset baseAsset, bool deriveTwice = true)
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var baseContainer = new AssetTestContainer<TAsset, TAssetPropertyGraph>(container, baseAsset);
            baseContainer.BuildGraph();
            var derivedAsset = (TAsset)baseContainer.Asset.CreateDerivedAsset("MyAsset");
            var derivedContainer = new AssetTestContainer<TAsset, TAssetPropertyGraph>(baseContainer.Container, derivedAsset);
            derivedContainer.BuildGraph();
            derivedContainer.Graph.RefreshBase();
            AssetTestContainer<TAsset, TAssetPropertyGraph> subDerivedContainer = null;
            if (deriveTwice)
            {
                var subDerivedAsset = (TAsset)derivedContainer.Asset.CreateDerivedAsset("MySubAsset");
                subDerivedContainer = new AssetTestContainer<TAsset, TAssetPropertyGraph>(baseContainer.Container, subDerivedAsset);
                subDerivedContainer.BuildGraph();
                subDerivedContainer.Graph.RefreshBase();
            }
            var result = new DeriveAssetTest<TAsset, TAssetPropertyGraph>(baseContainer, derivedContainer, subDerivedContainer);
            return result;
        }

        public static DeriveAssetTest<TAsset, TAssetPropertyGraph> LoadFromYaml(string baseYaml, string derivedYaml)
        {
            var container = new AssetPropertyGraphContainer(new AssetNodeContainer { NodeBuilder = { NodeFactory = new AssetNodeFactory() } });
            var baseAsset = AssetFileSerializer.Load<TAsset>(AssetTestContainer.ToStream(baseYaml), $"MyAsset{Types.FileExtension}");
            var derivedAsset = AssetFileSerializer.Load<TAsset>(AssetTestContainer.ToStream(derivedYaml), $"MyDerivedAsset{Types.FileExtension}");
            var baseContainer = new AssetTestContainer<TAsset, TAssetPropertyGraph>(container, baseAsset.Asset);
            var derivedContainer = new AssetTestContainer<TAsset, TAssetPropertyGraph>(container, derivedAsset.Asset);
            baseAsset.YamlMetadata.CopyInto(baseContainer.AssetItem.YamlMetadata);
            derivedAsset.YamlMetadata.CopyInto(derivedContainer.AssetItem.YamlMetadata);
            baseContainer.BuildGraph();
            derivedContainer.BuildGraph();
            var result = new DeriveAssetTest<TAsset, TAssetPropertyGraph>(baseContainer, derivedContainer, null);
            derivedContainer.Graph.RefreshBase();
            return result;
        }
    }
}
