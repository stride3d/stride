// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xunit;
using Stride.Core.Collections;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Tests
{
    [DataSerializerGlobal(null, typeof(KeyValuePair<int, int>))]
    public class TestStore
    {
        [Fact]
        public void ListSimple()
        {
            using (var tempFile = new TemporaryFile())
            using (var store2 = new ListStore<int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            using (var store1 = new ListStore<int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            {
                store1.UseTransaction = true;

                // Add a value to store2 and saves it
                store2.AddValue(1);
                store2.Save();

                // Add a value to store1 without saving
                store1.AddValue(2);
                Assert.Equal(new[] { 2 }, store1.GetValues());

                // Check that store1 contains value from store2 first
                store1.LoadNewValues();
                Assert.Equal(new[] { 1, 2 }, store1.GetValues());

                // Save and check that results didn't change
                store1.Save();
                Assert.Equal(new[] { 1, 2 }, store1.GetValues());
            }
        }

        [Fact]
        public void DictionarySimple()
        {
            using (var tempFile = new TemporaryFile())
            using (var store1 = new DictionaryStore<int, int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            using (var store2 = new DictionaryStore<int, int>(VirtualFileSystem.OpenStream(tempFile.Path, VirtualFileMode.OpenOrCreate, VirtualFileAccess.ReadWrite, VirtualFileShare.ReadWrite)))
            {
                store1.UseTransaction = true;

                // Check successive sets
                store1[1] = 1;
                Assert.Equal(1, store1[1]);

                store1[1] = 2;
                Assert.Equal(2, store1[1]);

                // Check saving (before and after completion)
                store1.Save();
                Assert.Equal(2, store1[1]);
                Assert.Equal(2, store1[1]);

                // Check set after save
                store1[1] = 3;
                Assert.Equal(3, store1[1]);

                // Check loading from another store
                store2.LoadNewValues();
                Assert.Equal(2, store2[1]);

                // Concurrent changes
                store1[1] = 5;
                store2[1] = 6;
                // Write should be scheduled for save immediately since dictionaryStore2 doesn't use transaction
                store2[2] = 6;

                // Check intermediate state (should get new value for 2, but keep intermediate non-saved value for 1)
                store1.LoadNewValues();
                Assert.Equal(5, store1[1]);
                Assert.Equal(6, store1[2]);

                // Check after save/reload, both stores should be synchronized
                store1.Save();
                store2.LoadNewValues();
                Assert.Equal(store2[1], store1[1]);
                Assert.Equal(store2[2], store1[2]);
            }
        }
    }
}
