// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Serialization.Contents;
using Stride.Core.Serialization.Assets;
using Stride.Core.Storage;
using Stride.Core.IO;

namespace Stride.Core.Tests.Build
{
    public class TestSerializer
    {
        private static void SaveSimpleAssets(AssetManager assetManager)
        {
            var simpleAsset = new SimpleAsset("Grandpa", new SimpleAsset("Pa", new SimpleAsset("Son", null)));
            assetManager.Save(simpleAsset);
        }

        [Fact]
        public unsafe void TestSaveAndLoadSimpleAssets()
        {
            var assetManager = new AssetManager();
            SaveSimpleAssets(assetManager);

            GC.Collect();

            var simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/Grandpa");
            Assert.Equal("SimpleAssets/Grandpa", simpleAsset.Url);
            Assert.Equal("Grandpa", simpleAsset.Str);
            Assert.Equal(5.0, simpleAsset.Dble);
            Assert.That(simpleAsset.Child, !Is.Null);

            Assert.Equal("SimpleAssets/Pa", simpleAsset.Child.Url);
            Assert.Equal("Pa", simpleAsset.Child.Str);
            Assert.Equal(5.0, simpleAsset.Child.Dble);
            Assert.That(simpleAsset.Child.Child, !Is.Null);

            Assert.Equal("SimpleAssets/Son", simpleAsset.Child.Child.Url);
            Assert.Equal("Son", simpleAsset.Child.Child.Str);
            Assert.Equal(5.0, simpleAsset.Child.Child.Dble);
            Assert.Null(simpleAsset.Child.Child.Child);
        }

        private static void SaveCyclicallyReferencedAssets(AssetManager assetManager)
        {
            var simpleAsset = new SimpleAsset("First", new SimpleAsset("Second", new SimpleAsset("Third", null)));
            simpleAsset.Child.Child.Child = simpleAsset;
            assetManager.Save(simpleAsset);
        }

        [Fact]
        public unsafe void TestSaveAndLoadCyclicallyReferencedAssets()
        {
            var assetManager = new AssetManager();
            SaveCyclicallyReferencedAssets(assetManager);

            GC.Collect();

            var simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/First");
            Assert.Equal("SimpleAssets/First", simpleAsset.Url);
            Assert.Equal("First", simpleAsset.Str);
            Assert.Equal(5.0, simpleAsset.Dble);
            Assert.That(simpleAsset.Child, !Is.Null);

            Assert.Equal("SimpleAssets/Second", simpleAsset.Child.Url);
            Assert.Equal("Second", simpleAsset.Child.Str);
            Assert.Equal(5.0, simpleAsset.Child.Dble);
            Assert.That(simpleAsset.Child.Child, !Is.Null);

            Assert.Equal("SimpleAssets/Third", simpleAsset.Child.Child.Url);
            Assert.Equal("Third", simpleAsset.Child.Child.Str);
            Assert.Equal(5.0, simpleAsset.Child.Child.Dble);
            Assert.That(simpleAsset.Child.Child.Child, Is.SameAs(simpleAsset));
        }


        private void SaveAssetsAndDeleteAChild(AssetManager assetManager)
        {
            var simpleAsset = new SimpleAsset("Pa", new SimpleAsset("Son", null));
            assetManager.Save(simpleAsset);
            var databaseFileProvider = (DatabaseFileProvider)VirtualFileSystem.ResolveProvider("/db", true).Provider;
            databaseFileProvider.AssetIndexMap.WaitPendingOperations();

            ObjectId childId;
            databaseFileProvider.AssetIndexMap.TryGetValue("SimpleAssets/Son", out childId);

            VirtualFileSystem.FileDelete(FileOdbBackend.BuildUrl(VirtualFileSystem.ApplicationDatabasePath, childId));
        }

        [Fact]
        public unsafe void TestLoadMissingAsset()
        {
            var assetManager = new AssetManager();
            var asset = assetManager.Load<SimpleAsset>("inexisting/asset");
            Assert.Null(asset);
            Assert.That(assetManager.HasAssetWithUrl("inexisting/asset"), Is.False);

            SaveAssetsAndDeleteAChild(assetManager);
            GC.Collect();

            asset = assetManager.Load<SimpleAsset>("SimpleAssets/Pa");
            Assert.That(asset, !Is.Null);
            Assert.Equal("SimpleAssets/Pa", asset.Url);
            Assert.Null(asset.Child);

            asset = assetManager.Load<SimpleAsset>("SimpleAssets/Son");
            Assert.Null(asset);
        }

        private void SaveComplexAssets(AssetManager assetManager)
        {
            var ass1 = new ComplexAsset("First");
            var ass2 = new ComplexAsset("Second");
            var ass3 = new ComplexAsset("Third");

            ass1.Children.Add(ass2);
            ass1.FirstChild = ass2;
            ass1.Data = new MemberData { Asset = ass2, Num = 1 };

            ass2.Children.Add(ass3);
            ass2.FirstChild = ass3;

            ass3.Children.Add(ass1);
            ass3.Children.Add(ass2);
            ass3.FirstChild = ass2;
            ass3.Data = new MemberData { Asset = ass1, Num = 2 };

            assetManager.Save(ass1);
        }

        [Fact]
        public unsafe void TestComplexAssets()
        {
            var assetManager = new AssetManager();
            SaveComplexAssets(assetManager);

            GC.Collect();

            var ass1 = assetManager.Load<ComplexAsset>("ComplexAssets/First");
            var ass2FromAss1 = ass1.FirstChild;
            var ass2 = assetManager.Load<ComplexAsset>("ComplexAssets/Second");
            var ass3FromAss2 = ass2.FirstChild;
            var ass3 = assetManager.Load<ComplexAsset>("ComplexAssets/Third");
            var ass2FromAss3 = ass3.FirstChild;

            Assert.Equal("ComplexAssets/First", ass1.Url);
            Assert.That(ass2FromAss1, Is.SameAs(ass1.FirstChild));
            Assert.That(ass2FromAss1, Is.SameAs(ass2));
            Assert.That(ass1.Data, !Is.Null);
            Assert.That(ass1.Data.Asset, Is.SameAs(ass2));
            Assert.Equal(1, ass1.Data.Num);
            Assert.Equal(1, ass1.Children.Count);
            Assert.That(ass1.Children[0], Is.SameAs(ass2));

            Assert.Equal("ComplexAssets/Second", ass2.Url);
            Assert.That(ass3FromAss2, Is.SameAs(ass2.FirstChild));
            Assert.That(ass3FromAss2, Is.SameAs(ass3));
            Assert.Null(ass2.Data);
            Assert.Equal(1, ass2.Children.Count);
            Assert.That(ass2.Children[0], Is.SameAs(ass3));

            Assert.Equal("ComplexAssets/Third", ass3.Url);
            Assert.That(ass2FromAss3, Is.SameAs(ass3.FirstChild));
            Assert.That(ass2FromAss3, Is.SameAs(ass2));
            Assert.That(ass3.Data, !Is.Null);
            Assert.That(ass3.Data.Asset, Is.SameAs(ass1));
            Assert.Equal(2, ass3.Data.Num);
            Assert.Equal(2, ass3.Children.Count);
            Assert.That(ass3.Children[0], Is.SameAs(ass1));
            Assert.That(ass3.Children[1], Is.SameAs(ass2));
        }

        private void SaveChangeResaveAssets(AssetManager assetManager)
        {
            var simpleAsset = new SimpleAsset("Grandpa", new SimpleAsset("Pa", new SimpleAsset("Son", null)));
            assetManager.Save(simpleAsset);
            simpleAsset.Dble = 22.0;
            simpleAsset.Child.Dble = 42.0;
            assetManager.SaveSingle(simpleAsset);
        }

        [Fact]
        public unsafe void TestSaveChangeResaveAndLoadSimpleAssets()
        {
            var assetManager = new AssetManager();
            SaveChangeResaveAssets(assetManager);

            GC.Collect();

            var simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/Grandpa");
            Assert.Equal("SimpleAssets/Grandpa", simpleAsset.Url);
            Assert.Equal("Grandpa", simpleAsset.Str);
            Assert.Equal(22.0, simpleAsset.Dble);
            Assert.That(simpleAsset.Child, !Is.Null);

            Assert.Equal("SimpleAssets/Pa", simpleAsset.Child.Url);
            Assert.Equal("Pa", simpleAsset.Child.Str);
            Assert.Equal(5.0, simpleAsset.Child.Dble);
            Assert.That(simpleAsset.Child.Dble, !Is.EqualTo(42.0));
            Assert.That(simpleAsset.Child.Child, !Is.Null);

            Assert.Equal("SimpleAssets/Son", simpleAsset.Child.Child.Url);
            Assert.Equal("Son", simpleAsset.Child.Child.Str);
            Assert.Equal(5.0, simpleAsset.Child.Child.Dble);
            Assert.Null(simpleAsset.Child.Child.Child);
        }

        [Fact]
        public unsafe void TestSaveAndLoadAssetManyTimes()
        {
            var assetManager = new AssetManager();
            var simpleAsset = new SimpleAsset("Grandpa", null) { Dble = 0.0 };
            assetManager.SaveSingle(simpleAsset);
            assetManager.Unload(simpleAsset);
            simpleAsset = null;

            GC.Collect();

            for (double d = 0; d < 10.0; ++d)
            {
                simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/Grandpa");
                Assert.Equal(d, simpleAsset.Dble);
                simpleAsset.Dble += 1.0;
                assetManager.SaveSingle(simpleAsset);
                assetManager.Unload(simpleAsset);
                simpleAsset = null;
                GC.Collect();
            }
        }

        [Fact]
        public unsafe void TestSaveAndLoadAssetAndIndexFileManyTimes()
        {
            var assetManager = new AssetManager();
            var simpleAsset = new SimpleAsset("Grandpa", null) { Dble = 0.0 };
            assetManager.SaveSingle(simpleAsset);
            assetManager.Unload(simpleAsset);
            var databaseFileProvider = (DatabaseFileProvider)VirtualFileSystem.ResolveProvider("/db", true).Provider;
            databaseFileProvider.AssetIndexMap.WaitPendingOperations();
            simpleAsset = null;

            GC.Collect();

            for (double d = 0; d < 10.0; ++d)
            {
                var anotherAssetManager = new AssetManager();
                simpleAsset = anotherAssetManager.Load<SimpleAsset>("SimpleAssets/Grandpa");
                Assert.Equal(d, simpleAsset.Dble);
                simpleAsset.Dble += 1.0;
                anotherAssetManager.SaveSingle(simpleAsset);
                anotherAssetManager.Unload(simpleAsset);
                databaseFileProvider.AssetIndexMap.WaitPendingOperations();
                simpleAsset = null;
                GC.Collect();
            }
        }
    }

    [ContentSerializer(typeof(DataContentSerializer<SimpleAsset>))]
    [Serializable]
    public class SimpleAsset : IContentUrl
    {
        public string Url { get; set; }
        public string Str;
        public double Dble;
        public int Intg;
        public SimpleAsset Child;
        private SimpleAsset currentChild;

        public SimpleAsset() { }

        public SimpleAsset(string str, SimpleAsset child)
        {
            Str = str;
            Dble = 5.0;
            Child = child;
            Url = "SimpleAssets/" + str;
        }
    }

    [ContentSerializer(typeof(DataContentSerializer<MemberData>))]
    [Serializable]
    public class MemberData
    {
        public ComplexAsset Asset;
        public int Num;
    }

    [ContentSerializer(typeof(DataContentSerializer<ComplexAsset>))]
    [Serializable]
    public class ComplexAsset : IContentUrl
    {
        public string Url { get; set; }
        public ComplexAsset FirstChild { get; set; }
        public List<ComplexAsset> Children { get; set; }
        public MemberData Data { get; set; }

        public ComplexAsset() { }

        public ComplexAsset(string url)
        {
            Children = new List<ComplexAsset>();
            Url = "ComplexAssets/" + url;
        }
    }
}
