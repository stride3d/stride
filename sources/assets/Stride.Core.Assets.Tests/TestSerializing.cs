// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Assets.Yaml;
using Stride.Core.IO;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Tests
{
    public partial class TestSerializing : TestBase
    {
        static TestSerializing()
        {
            AssemblyRegistry.Register(typeof(TestSerializing).Assembly, AssemblyCommonCategories.Assets);
        }

        [Fact]
        public void TestMyAssetObject()
        {
            var assetObject = new MyAsset();
            assetObject.Id = AssetId.Empty;

            assetObject.Description = "This is a test";

            assetObject.AssetDirectory = new UDirectory("/test/dynamic/path/to/file/in/object/property");
            assetObject.AssetUrl = new UFile("/test/dynamic/path/to/file/in/object/property");

            //assetObject.Base = new AssetBase("/this/is/an/url/to/MyObject", null);

            assetObject.CustomReference2 = new AssetReference(AssetId.Empty, "/this/is/an/url/to/MyCustomReference2");
            assetObject.CustomReferences.Add(new AssetReference(AssetId.Empty, "/this/is/an/url/to/MyCustomReferenceItem1"));
            var ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.CustomReferences);
            ids[0] = IdentifierGenerator.Get(99);

            assetObject.SeqItems1.Add("value1");
            assetObject.SeqItems1.Add("value2");
            ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.SeqItems1);
            ids[0] = IdentifierGenerator.Get(1);
            ids[1] = IdentifierGenerator.Get(2);

            // TODO: Re-enable non-pure collections here once we support them for serialization!
            //assetObject.SeqItems2.Add("value1");
            //assetObject.SeqItems2.Add("value2");
            //assetObject.SeqItems2.Add("value3");
            //ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.SeqItems2);
            //ids[0] = IdentifierGenerator.Get(3);
            //ids[1] = IdentifierGenerator.Get(4);
            //ids[2] = IdentifierGenerator.Get(5);

            assetObject.SeqItems3.Add("value1");
            assetObject.SeqItems3.Add("value2");
            assetObject.SeqItems3.Add("value3");
            assetObject.SeqItems3.Add("value4");
            ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.SeqItems3);
            ids[0] = IdentifierGenerator.Get(6);
            ids[1] = IdentifierGenerator.Get(7);
            ids[2] = IdentifierGenerator.Get(8);
            ids[3] = IdentifierGenerator.Get(9);

            assetObject.SeqItems4.Add("value0");
            ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.SeqItems4);
            ids[0] = IdentifierGenerator.Get(10);

            // TODO: Re-enable non-pure collections here once we support them for serialization!
            //assetObject.SeqItems5.Add("value0");
            //ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.SeqItems5);
            //ids[0] = IdentifierGenerator.Get(11);

            assetObject.MapItems1.Add("key1", 1);
            assetObject.MapItems1.Add("key2", 2);
            ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.MapItems1);
            ids["key1"] = IdentifierGenerator.Get(12);
            ids["key2"] = IdentifierGenerator.Get(13);

            // TODO: Re-enable non-pure collections here once we support them for serialization!
            //assetObject.MapItems2.Add("key1", 1);
            //assetObject.MapItems2.Add("key2", 2);
            //assetObject.MapItems2.Add("key3", 3);
            //ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.MapItems2);
            //ids["key1"] = IdentifierGenerator.Get(13);
            //ids["key2"] = IdentifierGenerator.Get(14);
            //ids["key3"] = IdentifierGenerator.Get(15);

            assetObject.MapItems3.Add("key1", 1);
            assetObject.MapItems3.Add("key2", 2);
            assetObject.MapItems3.Add("key3", 3);
            assetObject.MapItems3.Add("key4", 3);
            ids = CollectionItemIdHelper.GetCollectionItemIds(assetObject.MapItems3);
            ids["key1"] = IdentifierGenerator.Get(16);
            ids["key2"] = IdentifierGenerator.Get(17);
            ids["key3"] = IdentifierGenerator.Get(18);
            ids["key4"] = IdentifierGenerator.Get(19);

            string testGenerated1 = DirectoryTestBase + @"TestSerializing\TestSerializing_TestMyAssetObject_Generated1.sdobj";
            string testGenerated2 = DirectoryTestBase + @"TestSerializing\TestSerializing_TestMyAssetObject_Generated2.sdobj";
            string referenceFilePath = DirectoryTestBase + @"TestSerializing\TestSerializing_TestMyAssetObject_Reference.sdobj";

            // First store the file on the disk and compare it to the reference
            GenerateAndCompare("Test Serialization 1", testGenerated1, referenceFilePath, assetObject);

            // Deserialize it
            var newAssetObject = AssetFileSerializer.Load<MyAsset>(testGenerated1).Asset;

            // Restore the deserialize version and compare it with the reference
            GenerateAndCompare("Test Serialization 2 - double check", testGenerated2, referenceFilePath, newAssetObject);
        }


        [Fact]
        public void TestAssetItemCollection()
        {
            // Test serialization of asset items.

            var inputs = new List<AssetItem>();
            for (int i = 0; i < 10; i++)
            {
                var newAsset = new AssetObjectTest() { Name = "Test" + i };
                inputs.Add(new AssetItem("" + i, newAsset));
            }

            var asText = ToText(inputs);
            var outputs = FromText(asText);

            Assert.Equal(inputs.Select(item => item.Location), outputs.Select(item => item.Location));
            Assert.Equal(inputs.Select(item => item.Asset), outputs.Select(item => item.Asset));
        }

        private static string ToText(List<AssetItem> assetCollection)
        {
            var stream = new MemoryStream();
            AssetFileSerializer.Default.Save(stream, assetCollection, null);
            stream.Position = 0;
            return new StreamReader(stream).ReadToEnd();
        }

        private static List<AssetItem> FromText(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;

            bool aliasOccurred;
            AttachedYamlAssetMetadata metadata;
            var assetItems = (List<AssetItem>)AssetFileSerializer.Default.Load(stream, null, null, true, out aliasOccurred, out metadata);
            if (aliasOccurred)
            {
                foreach (var assetItem in assetItems)
                {
                    assetItem.IsDirty = true;
                }
            }
            return assetItems;
        }
    }
}
