// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Xenko.Core.IO;
using Xenko.Core.Storage;

namespace Xenko.Core.Assets.Tests
{
    [TestFixture]
    public class TestFileVersionManager : TestBase
    {
        public string TestDirectory
        {
            get
            {
                return Path.Combine(DirectoryTestBase, "TestFileVersionManager");
            }
        }

        [Test]
        public void Test()
        {
            var path = Path.Combine(TestDirectory, "test.txt");

            var objectId1 = FileVersionManager.Instance.ComputeFileHash(path);
            var objectId2 = FileVersionManager.Instance.ComputeFileHash(path);
            Assert.AreNotEqual(ObjectId.Empty, objectId1);
            Assert.AreEqual(objectId1, objectId2);

            File.SetLastWriteTime(path, DateTime.Now);

            var objectId3 = FileVersionManager.Instance.ComputeFileHash(path);
            var objectId4 = FileVersionManager.Instance.ComputeFileHash(path);
            Assert.AreNotEqual(ObjectId.Empty, objectId3);
            Assert.AreEqual(objectId3, objectId4);
            Assert.AreEqual(objectId1, objectId3);

            FileVersionManager.Shutdown();
        }


        [Test]
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

            Assert.AreEqual(files.Count, ids.Count);

            var objectId1 = FileVersionManager.Instance.ComputeFileHash(files[0]);
            Assert.AreNotEqual(ObjectId.Empty, objectId1);
            Assert.AreEqual(objectId1, ids[0].Item2);

            FileVersionManager.Shutdown();
        }
    }
}
