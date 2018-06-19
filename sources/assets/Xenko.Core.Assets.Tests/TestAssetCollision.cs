// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using Xenko.Core.Assets.Analysis;
using Xenko.Core.IO;

namespace Xenko.Core.Assets.Tests
{
    [TestFixture]
    public class TestAssetCollision
    {
        [Test]
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
            Assert.AreEqual(inputs.Count, outputs.Count);

            // Make sure that asset has been cloned
            Assert.AreNotEqual(inputs[0], outputs[0]);

            // First Id should not change
            Assert.AreEqual(inputs[0].Id, outputs[0].Id);

            // Make sure that all ids are different
            var ids = new HashSet<AssetId>(outputs.Select(item => item.Id));
            Assert.AreEqual(inputs.Count, ids.Count);

            // Make sure that all locations are different
            var locations = new HashSet<UFile>(outputs.Select(item => item.Location));
            Assert.AreEqual(inputs.Count, locations.Count);

            // Reference location "bad"should be fixed to "0"
            foreach (var output in outputs)
            {
                var assetRef = ((AssetObjectTest)output.Asset).Reference;
                Assert.AreEqual("0", assetRef.Location);
                Assert.AreEqual(outputs[0].Id, assetRef.Id);
            }
        }

        [Test]
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
            Assert.AreEqual(inputs.Count, outputs.Count);

            // Make sure that asset has been cloned
            Assert.AreNotEqual(inputs[0], outputs[0]);

            // First Id should not change
            Assert.AreNotEqual(inputs[0].Id, outputs[0].Id);

            // Make sure that all ids are different
            var ids = new HashSet<AssetId>(outputs.Select(item => item.Id));
            Assert.AreEqual(inputs.Count, ids.Count);

            // Make sure that all locations are different
            var locations = new HashSet<UFile>(outputs.Select(item => item.Location));
            Assert.AreEqual(inputs.Count, locations.Count);

            // Reference location "bad"should be fixed to "0"
            foreach (var output in outputs)
            {
                var assetRef = ((AssetObjectTest)output.Asset).Reference;
                Assert.AreEqual("0", assetRef.Location);
                Assert.AreEqual(outputs[0].Id, assetRef.Id);
            }
        }

        [Test]
        public void TestWithPackage()
        {
            var inputs = new List<AssetItem>();

            var asset = new AssetObjectTest();

            var package = new Package();
            package.Assets.Add(new AssetItem("0", asset));

            for (int i = 0; i < 10; i++)
            {
                var newAsset = new AssetObjectTest() { Id = asset.Id, Reference = new AssetReference(asset.Id, "bad") };
                inputs.Add(new AssetItem("0", newAsset));
            }

            // Tries to use existing ids
            var outputs = new List<AssetItem>();
            AssetCollision.Clean(null, inputs, outputs, AssetResolver.FromPackage(package), true, false);

            // Make sure we are generating exactly the same number of elements
            Assert.AreEqual(inputs.Count, outputs.Count);

            // Make sure that asset has been cloned
            Assert.AreNotEqual(inputs[0], outputs[0]);

            // First Id should not change
            Assert.AreNotEqual(inputs[0].Id, outputs[0].Id);

            // Make sure that all ids are different
            var ids = new HashSet<AssetId>(outputs.Select(item => item.Id));
            Assert.AreEqual(inputs.Count, ids.Count);

            // Make sure that all locations are different
            var locations = new HashSet<UFile>(outputs.Select(item => item.Location));
            Assert.AreEqual(inputs.Count, locations.Count);

            // Reference location "bad"should be fixed to "0_1" pointing to the first element
            foreach (var output in outputs)
            {
                // Make sure of none of the locations are using "0"
                Assert.AreNotEqual("0", output.Location);

                var assetRef = ((AssetObjectTest)output.Asset).Reference;
                Assert.AreEqual("0 (2)", assetRef.Location);
                Assert.AreEqual(outputs[0].Id, assetRef.Id);
            }
        }
    }
}
