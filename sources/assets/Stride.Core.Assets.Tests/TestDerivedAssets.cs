// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Assets.Serializers;
using Stride.Core.Assets.Yaml;
using Stride.Core;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Assets.Tests
{
    public class TestDerivedAssets
    {
        [DataContract]
        public class MyObject
        {
            public List<string> Strings { get; set; } = new List<string>();
            public List<MyObject> Objects { get; set; } = new List<MyObject>();
            public Dictionary<string, MyObject> StringsAndObjects { get; set; } = new Dictionary<string, MyObject>();
        }

        [DataContract]
        public class MyAsset : Asset
        {
            public List<string> Strings { get; set; } = new List<string>();
            public List<MyObject> Objects { get; set; } = new List<MyObject>();
            public Dictionary<string, MyObject> StringsAndObjects { get; set; } = new Dictionary<string, MyObject>();
        }

        private static byte[] MakeArray(byte b)
        {
            var array = new byte[16];
            for (var i = 0; i < 16; ++i)
            {
                array[i] = b;
            }
            return array;
        }

        private static ItemId MakeItemId(byte b)
        {
            var array = new byte[16];
            for (var i = 0; i < 16; ++i)
            {
                array[i] = b;
            }
            return new ItemId(array);
        }

        [Fact]
        public void TestSimpleSerialization()
        {
            var asset = new MyAsset { Strings = { "aa", "bb" } };
            var asset1Ids = CollectionItemIdHelper.GetCollectionItemIds(asset.Strings);
            asset1Ids.Add(0, new ItemId(MakeArray(1)));
            asset1Ids.Add(1, new ItemId(MakeArray(2)));
            var serializer = new YamlAssetSerializer();
            var stream = new MemoryStream();
            serializer.Save(stream, asset, null);
            stream.Position = 0;
            bool aliasOccurred;
            AttachedYamlAssetMetadata metadata;
            var loadedAsset = (MyAsset)serializer.Load(stream, null, null, true, out aliasOccurred, out metadata);
            var asset2Ids = CollectionItemIdHelper.GetCollectionItemIds(loadedAsset.Strings);
            Assert.Equal(2, asset2Ids.KeyCount);
            Assert.Equal(asset1Ids[0], asset2Ids[0]);
            Assert.Equal(asset1Ids[1], asset2Ids[1]);
        }

    }
}
