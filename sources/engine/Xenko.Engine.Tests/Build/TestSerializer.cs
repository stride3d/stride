// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Serialization.Contents;
using Xenko.Core.Serialization.Assets;
using Xenko.Core.Storage;
using Xenko.Core.IO;

namespace Xenko.Core.Tests.Build
{
    [TestFixture]
    public class TestSerializer
    {
        private static void SaveSimpleAssets(AssetManager assetManager)
        {
            var simpleAsset = new SimpleAsset("Grandpa", new SimpleAsset("Pa", new SimpleAsset("Son", null)));
            assetManager.Save(simpleAsset);
        }

        [Test]
        public unsafe void TestSaveAndLoadSimpleAssets()
        {
            var assetManager = new AssetManager();
            SaveSimpleAssets(assetManager);

            GC.Collect();

            var simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/Grandpa");
            Assert.That(simpleAsset.Url, Is.EqualTo("SimpleAssets/Grandpa"));
            Assert.That(simpleAsset.Str, Is.EqualTo("Grandpa"));
            Assert.That(simpleAsset.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child, !Is.Null);

            Assert.That(simpleAsset.Child.Url, Is.EqualTo("SimpleAssets/Pa"));
            Assert.That(simpleAsset.Child.Str, Is.EqualTo("Pa"));
            Assert.That(simpleAsset.Child.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child.Child, !Is.Null);

            Assert.That(simpleAsset.Child.Child.Url, Is.EqualTo("SimpleAssets/Son"));
            Assert.That(simpleAsset.Child.Child.Str, Is.EqualTo("Son"));
            Assert.That(simpleAsset.Child.Child.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child.Child.Child, Is.Null);
        }

        private static void SaveCyclicallyReferencedAssets(AssetManager assetManager)
        {
            var simpleAsset = new SimpleAsset("First", new SimpleAsset("Second", new SimpleAsset("Third", null)));
            simpleAsset.Child.Child.Child = simpleAsset;
            assetManager.Save(simpleAsset);
        }

        [Test]
        public unsafe void TestSaveAndLoadCyclicallyReferencedAssets()
        {
            var assetManager = new AssetManager();
            SaveCyclicallyReferencedAssets(assetManager);

            GC.Collect();

            var simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/First");
            Assert.That(simpleAsset.Url, Is.EqualTo("SimpleAssets/First"));
            Assert.That(simpleAsset.Str, Is.EqualTo("First"));
            Assert.That(simpleAsset.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child, !Is.Null);

            Assert.That(simpleAsset.Child.Url, Is.EqualTo("SimpleAssets/Second"));
            Assert.That(simpleAsset.Child.Str, Is.EqualTo("Second"));
            Assert.That(simpleAsset.Child.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child.Child, !Is.Null);

            Assert.That(simpleAsset.Child.Child.Url, Is.EqualTo("SimpleAssets/Third"));
            Assert.That(simpleAsset.Child.Child.Str, Is.EqualTo("Third"));
            Assert.That(simpleAsset.Child.Child.Dble, Is.EqualTo(5.0));
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

        [Test]
        public unsafe void TestLoadMissingAsset()
        {
            var assetManager = new AssetManager();
            var asset = assetManager.Load<SimpleAsset>("inexisting/asset");
            Assert.That(asset, Is.Null);
            Assert.That(assetManager.HasAssetWithUrl("inexisting/asset"), Is.False);

            SaveAssetsAndDeleteAChild(assetManager);
            GC.Collect();

            asset = assetManager.Load<SimpleAsset>("SimpleAssets/Pa");
            Assert.That(asset, !Is.Null);
            Assert.That(asset.Url, Is.EqualTo("SimpleAssets/Pa"));
            Assert.That(asset.Child, Is.Null);

            asset = assetManager.Load<SimpleAsset>("SimpleAssets/Son");
            Assert.That(asset, Is.Null);
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

        [Test]
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

            Assert.That(ass1.Url, Is.EqualTo("ComplexAssets/First"));
            Assert.That(ass2FromAss1, Is.SameAs(ass1.FirstChild));
            Assert.That(ass2FromAss1, Is.SameAs(ass2));
            Assert.That(ass1.Data, !Is.Null);
            Assert.That(ass1.Data.Asset, Is.SameAs(ass2));
            Assert.That(ass1.Data.Num, Is.EqualTo(1));
            Assert.That(ass1.Children.Count, Is.EqualTo(1));
            Assert.That(ass1.Children[0], Is.SameAs(ass2));

            Assert.That(ass2.Url, Is.EqualTo("ComplexAssets/Second"));
            Assert.That(ass3FromAss2, Is.SameAs(ass2.FirstChild));
            Assert.That(ass3FromAss2, Is.SameAs(ass3));
            Assert.That(ass2.Data, Is.Null);
            Assert.That(ass2.Children.Count, Is.EqualTo(1));
            Assert.That(ass2.Children[0], Is.SameAs(ass3));

            Assert.That(ass3.Url, Is.EqualTo("ComplexAssets/Third"));
            Assert.That(ass2FromAss3, Is.SameAs(ass3.FirstChild));
            Assert.That(ass2FromAss3, Is.SameAs(ass2));
            Assert.That(ass3.Data, !Is.Null);
            Assert.That(ass3.Data.Asset, Is.SameAs(ass1));
            Assert.That(ass3.Data.Num, Is.EqualTo(2));
            Assert.That(ass3.Children.Count, Is.EqualTo(2));
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

        [Test]
        public unsafe void TestSaveChangeResaveAndLoadSimpleAssets()
        {
            var assetManager = new AssetManager();
            SaveChangeResaveAssets(assetManager);

            GC.Collect();

            var simpleAsset = assetManager.Load<SimpleAsset>("SimpleAssets/Grandpa");
            Assert.That(simpleAsset.Url, Is.EqualTo("SimpleAssets/Grandpa"));
            Assert.That(simpleAsset.Str, Is.EqualTo("Grandpa"));
            Assert.That(simpleAsset.Dble, Is.EqualTo(22.0));
            Assert.That(simpleAsset.Child, !Is.Null);

            Assert.That(simpleAsset.Child.Url, Is.EqualTo("SimpleAssets/Pa"));
            Assert.That(simpleAsset.Child.Str, Is.EqualTo("Pa"));
            Assert.That(simpleAsset.Child.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child.Dble, !Is.EqualTo(42.0));
            Assert.That(simpleAsset.Child.Child, !Is.Null);

            Assert.That(simpleAsset.Child.Child.Url, Is.EqualTo("SimpleAssets/Son"));
            Assert.That(simpleAsset.Child.Child.Str, Is.EqualTo("Son"));
            Assert.That(simpleAsset.Child.Child.Dble, Is.EqualTo(5.0));
            Assert.That(simpleAsset.Child.Child.Child, Is.Null);
        }

        [Test]
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
                Assert.That(simpleAsset.Dble, Is.EqualTo(d));
                simpleAsset.Dble += 1.0;
                assetManager.SaveSingle(simpleAsset);
                assetManager.Unload(simpleAsset);
                simpleAsset = null;
                GC.Collect();
            }
        }

        [Test]
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
                Assert.That(simpleAsset.Dble, Is.EqualTo(d));
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
