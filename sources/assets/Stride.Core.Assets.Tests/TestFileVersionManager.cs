// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Xunit;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Tests
{
    public class TestFileVersionManager : TestBase
    {
        public string TestDirectory
        {
            get
            {
                return Path.Combine(DirectoryTestBase, "TestFileVersionManager");
            }
        }

        [Fact]
        public void Test()
        {
            var path = Path.Combine(TestDirectory, "test.txt");

            var objectId1 = FileVersionManager.Instance.ComputeFileHash(path);
            var objectId2 = FileVersionManager.Instance.ComputeFileHash(path);
            Assert.NotEqual(ObjectId.Empty, objectId1);
            Assert.Equal(objectId1, objectId2);

            File.SetLastWriteTime(path, DateTime.Now);

            var objectId3 = FileVersionManager.Instance.ComputeFileHash(path);
            var objectId4 = FileVersionManager.Instance.ComputeFileHash(path);
            Assert.NotEqual(ObjectId.Empty, objectId3);
            Assert.Equal(objectId3, objectId4);
            Assert.Equal(objectId1, objectId3);

            FileVersionManager.Shutdown();
        }


        [Fact]
        public void TestAsync()
        {
            var files = new List<UFile>();
            for (int i = 0; i < 10; i++)
            {
                var path = Path.Combine(TestDirectory, "test_random" + i + ".txt");
                File.WriteAllText(path, "Random " + i);
                files.Add(path);
            }

            var ids = new List<Tuple<UFile, ObjectId>>();
            FileVersionManager.Instance.ComputeFileHashAsync(files, (file, id) => ids.Add(new Tuple<UFile, ObjectId>(file, id)));
            Thread.Sleep(200);

            Assert.Equal(files.Count, ids.Count);

            var objectId1 = FileVersionManager.Instance.ComputeFileHash(files[0]);
            Assert.NotEqual(ObjectId.Empty, objectId1);
            Assert.Equal(objectId1, ids[0].Item2);

            FileVersionManager.Shutdown();
        }
    }
}
