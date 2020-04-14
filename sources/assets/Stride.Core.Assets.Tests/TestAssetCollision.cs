// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

using Stride.Core.Assets.Analysis;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests
{
    public class TestAssetCollision
    {
        [Fact]
        public void TestSimple()
        {
            var inputs = new List<AssetItem>();

            var asset = new AssetObjectTest();
            for (int i = 0; i < 10; i++)
            {
                var newAsset = new AssetObjectTest() { Id = asset.Id, Reference =  new AssetReference(asset.Id, "bad")};
                inputs.Add(new AssetItem("0", newAsset));
            }

            // Tries to use existing ids
            var outputs = new List<AssetItem>();
            AssetCollision.Clean(null, inputs, outputs, new AssetResolver(), true, false);

            // Make sure we are generating exactly the same number of elements
            Assert.Equal(inputs.Count, outputs.Count);

            // Make sure that asset has been cloned
            Assert.NotEqual(inputs[0], outputs[0]);

            // First Id should not change
            Assert.Equal(inputs[0].Id, outputs[0].Id);

            // Make sure that all ids are different
            var ids = new HashSet<AssetId>(outputs.Select(item => item.Id));
            Assert.Equal(inputs.Count, ids.Count);

            // Make sure that all locations are different
            var locations = new HashSet<UFile>(outputs.Select(item => item.Location));
            Assert.Equal(inputs.Count, locations.Count);

            // Reference location "bad"should be fixed to "0"
            foreach (var output in outputs)
            {
                var assetRef = ((AssetObjectTest)output.Asset).Reference;
                Assert.Equal("0", assetRef.Location);
                Assert.Equal(outputs[0].Id, assetRef.Id);
            }
        }

        [Fact]
        public void TestSimpleNewGuids()
        {
            var inputs = new List<AssetItem>();

            var asset = new AssetObjectTest();
            for (int i = 0; i < 10; i++)
            {
                var newAsset = new AssetObjectTest() { Id = asset.Id, Reference = new AssetReference(asset.Id, "bad") };
                inputs.Add(new AssetItem("0", newAsset));
            }

            // Force to use new ids
            var outputs = new List<AssetItem>();
            AssetCollision.Clean(null, inputs, outputs, new AssetResolver() { AlwaysCreateNewId = true }, true, false);

            // Make sure we are generating exactly the same number of elements
            Assert.Equal(inputs.Count, outputs.Count);

            // Make sure that asset has been cloned
            Assert.NotEqual(inputs[0], outputs[0]);

            // First Id should not change
            Assert.NotEqual(inputs[0].Id, outputs[0].Id);

            // Make sure that all ids are different
            var ids = new HashSet<AssetId>(outputs.Select(item => item.Id));
            Assert.Equal(inputs.Count, ids.Count);

            // Make sure that all locations are different
            var locations = new HashSet<UFile>(outputs.Select(item => item.Location));
            Assert.Equal(inputs.Count, locations.Count);

            // Reference location "bad"should be fixed to "0"
            foreach (var output in outputs)
            {
                var assetRef = ((AssetObjectTest)output.Asset).Reference;
                Assert.Equal("0", assetRef.Location);
                Assert.Equal(outputs[0].Id, assetRef.Id);
            }
        }

        [Fact]
        public void TestWithPackage()
        {
            var inputs = new List<AssetItem>();

            var asset = new AssetObjectTest();

            var package = new Package();
            package.Assets.Add(new AssetItem("0", asset));
            var session = new PackageSession(package);

            for (int i = 0; i < 10; i++)
            {
                var newAsset = new AssetObjectTest() { Id = asset.Id, Reference = new AssetReference(asset.Id, "bad") };
                inputs.Add(new AssetItem("0", newAsset));
            }

            // Tries to use existing ids
            var outputs = new List<AssetItem>();
            AssetCollision.Clean(null, inputs, outputs, AssetResolver.FromPackage(package), true, false);

            // Make sure we are generating exactly the same number of elements
            Assert.Equal(inputs.Count, outputs.Count);

            // Make sure that asset has been cloned
            Assert.NotEqual(inputs[0], outputs[0]);

            // First Id should not change
            Assert.NotEqual(inputs[0].Id, outputs[0].Id);

            // Make sure that all ids are different
            var ids = new HashSet<AssetId>(outputs.Select(item => item.Id));
            Assert.Equal(inputs.Count, ids.Count);

            // Make sure that all locations are different
            var locations = new HashSet<UFile>(outputs.Select(item => item.Location));
            Assert.Equal(inputs.Count, locations.Count);

            // Reference location "bad"should be fixed to "0_1" pointing to the first element
            foreach (var output in outputs)
            {
                // Make sure of none of the locations are using "0"
                Assert.NotEqual((UFile)"0", output.Location);

                var assetRef = ((AssetObjectTest)output.Asset).Reference;
                Assert.Equal("0 (2)", assetRef.Location);
                Assert.Equal(outputs[0].Id, assetRef.Id);
            }
        }
    }
}
