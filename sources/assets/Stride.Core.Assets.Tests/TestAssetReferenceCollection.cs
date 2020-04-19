// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xunit;

using Stride.Core.IO;

namespace Stride.Core.Assets.Tests
{
    public class TestAssetReferenceCollection
    {
        [Fact]
        public void TestCollectionAddRemove()
        {
            // TODO test to be modified
           /*

            var assets = new AssetItemCollection();

            // Check that null are not allowed
            Assert.Throws<ArgumentNullException>(() => assets.Add(null));

            // Check that null location are not allowed
            Assert.Throws<ArgumentNullException>(() => assets.Add(new AssetItem(null, null)));

            // Test Find
            var ref1 = new AssetItem("a/test.txt", null);
            assets.Add(ref1);

            var ref2 = new AssetItem("b/test.txt", null);
            assets.Add(ref2);

            var findRef1 = assets.Find("a/test");
            Assert.Equal(ref1, findRef1);

            // Test Remove
            assets.Remove(ref1);
            Assert.Equal(assets.Count, 1);

            // Change location after adding an asset reference
            //ref1.Location = "a/test2.txt";
            assets.Add(ref1);
            //ref1.Location = "a/test3.txt";

            findRef1 = assets.Find("a/test3");
            Assert.Equal(ref1, findRef1);
            Assert.Equal(assets.Count, 2);

            // Add a reference with the same name
            Assert.Throws<ArgumentException>(() => assets.Add(new AssetItem("a/test3.png", null)));
            Assert.Equal(assets.Count, 2);

            // Test clear
            assets.Clear();
            Assert.Equal(assets.Count, 0);
            * */
        }
    }
}
