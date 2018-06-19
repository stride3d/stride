// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;
#if XENKO_PLATFORM_WINDOWS_DESKTOP
using System;
using System.IO;
using System.Text;

using NUnit.Framework;
using Xenko.Core.Storage;
using Xenko.Core.IO;

namespace Xenko.Core.Tests.Build
{
    [TestFixture, Ignore("Need check")]
    public class TestStorage
    {
        private readonly int[] blobData1 = new[] { 0, 1, 2, 3 };
        private readonly int[] blobData2 = new[] { 0, 2, 3, 4 };

        [Test]
        public unsafe void TestBlobSizeAndData()
        {
            using (var tempObjDatabase = new TemporaryObjectDatabase())
            {
                var objectDatabase = tempObjDatabase.ObjectDatabase;
                Blob blob;
                fixed (int* blobData1Ptr = blobData1)
                    blob = objectDatabase.CreateBlob((IntPtr)blobData1Ptr, Utilities.SizeOf(blobData1));

                // Check size
                Assert.That(blob.Size, Is.EqualTo(Utilities.SizeOf(blobData1)));

                // Check content
                fixed (int* blobData1Ptr = blobData1)
                    Assert.That(Utilities.CompareMemory(blob.Content, (IntPtr)blobData1Ptr, Utilities.SizeOf(blobData1)));

                // Test unloading blob
                Assert.That(blob.Release(), Is.EqualTo(0));
            }
        }

        [Test]
        public unsafe void TestBlobLookup()
        {
            using (var tempObjDatabase = new TemporaryObjectDatabase())
            {
                var objectDatabase = tempObjDatabase.ObjectDatabase;
                Blob blob;
                fixed (int* blobData1Ptr = blobData1)
                    blob = objectDatabase.CreateBlob((IntPtr)blobData1Ptr, Utilities.SizeOf(blobData1));

                // Check that loading the same ObjectId returns the same blob.
                var blob2 = objectDatabase.Lookup(blob.ObjectId);
                Assert.That(blob2, Is.EqualTo(blob));
                blob2.Release();

                // Check lookup of inexisting blob
                var blob3 = objectDatabase.Lookup(new ObjectId(new byte[20]));
                Assert.That(blob3, Is.Null);

                // Test unloading blob
                Assert.That(blob.Release(), Is.EqualTo(0));

                // Lookup blob again (it should load again from disk)
                var blob4 = objectDatabase.Lookup(blob.ObjectId);

                // Reference should be new
                Assert.That(blob4, Is.Not.EqualTo(blob));
                // Check size
                Assert.That(blob4.Size, Is.EqualTo(Utilities.SizeOf(blobData1)));
                // Check content
                fixed (int* blobData1Ptr = blobData1)
                    Assert.That(Utilities.CompareMemory(blob4.Content, (IntPtr)blobData1Ptr, Utilities.SizeOf(blobData1)));
                // Unload
                Assert.That(blob4.Release(), Is.EqualTo(0));
            }
        }

        [Test]
        public unsafe void TestBlobIdentical()
        {
            using (var tempObjDatabase = new TemporaryObjectDatabase())
            {
                var objectDatabase = tempObjDatabase.ObjectDatabase;
                Blob blob, blob2, blob3;

                // Test that loading a blob with the same data gives the same result.
                fixed (int* blobData1Ptr = blobData1)
                fixed (int* blobData2Ptr = blobData2)
                {
                    blob = objectDatabase.CreateBlob((IntPtr)blobData1Ptr, sizeof(int) * blobData1.Length);
                    blob2 = objectDatabase.CreateBlob((IntPtr)blobData1Ptr, sizeof(int) * blobData1.Length);
                    blob3 = objectDatabase.CreateBlob((IntPtr)blobData2Ptr, sizeof(int) * blobData2.Length);
                }

                Assert.That(blob2, Is.EqualTo(blob));
                Assert.That(blob3, Is.Not.EqualTo(blob));
                blob2.Release();
                blob3.Release();

                // Test unloading blob
                Assert.That(blob.Release(), Is.EqualTo(0));
            }
        }

        [Test]
        public unsafe void TestDigestStream()
        {
            string s1 = "abc";
            string s2 = "abcdefghijklmnopqrstuvwxyz";
            string s3 = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz123456789";

            ObjectId xenkoHash;
            ObjectId dotNetHash;
            using (var ds = new DigestStream(new MemoryStream()))
            {
                ds.Write(Encoding.ASCII.GetBytes(s1), 0, s1.Length);
                xenkoHash = ds.CurrentHash;
            }
            using (var hashAlgorithm = new System.Security.Cryptography.SHA1Managed())
            {
                dotNetHash = new ObjectId(hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(s1)));
            }
            Assert.That(xenkoHash, Is.EqualTo(dotNetHash));

            using (var ds = new DigestStream(new MemoryStream()))
            {
                ds.Write(Encoding.ASCII.GetBytes(s2), 0, s2.Length);
                xenkoHash = ds.CurrentHash;
            }
            using (var hashAlgorithm = new System.Security.Cryptography.SHA1Managed())
            {
                dotNetHash = new ObjectId(hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(s2)));
            }
            Assert.That(xenkoHash, Is.EqualTo(dotNetHash));

            using (var ds = new DigestStream(new MemoryStream()))
            {
                ds.Write(Encoding.ASCII.GetBytes(s3), 0, s3.Length);
                xenkoHash = ds.CurrentHash;
            }
            using (var hashAlgorithm = new System.Security.Cryptography.SHA1Managed())
            {
                dotNetHash = new ObjectId(hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(s3)));
            }
            Assert.That(xenkoHash, Is.EqualTo(dotNetHash));

            using (var ds = new DigestStream(new MemoryStream()))
            {
                ds.Write(Encoding.ASCII.GetBytes(s1), 0, s1.Length);
                ds.Write(Encoding.ASCII.GetBytes(s2), 0, s2.Length);
                ds.Write(Encoding.ASCII.GetBytes(s3), 0, s3.Length);
                xenkoHash = ds.CurrentHash;
            }
            using (var hashAlgorithm = new System.Security.Cryptography.SHA1Managed())
            {
                dotNetHash = new ObjectId(hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(s1 + s2 + s3)));
            }
            Assert.That(xenkoHash, Is.EqualTo(dotNetHash));
        }

        class TemporaryObjectDatabase : IDisposable
        {
            private TemporaryDirectory temporaryDirectory = new TemporaryDirectory();
            public ObjectDatabase ObjectDatabase;

            public TemporaryObjectDatabase()
            {
                VirtualFileSystem.MountFileSystem("/storage_test", temporaryDirectory.DirectoryPath);
                ObjectDatabase = new ObjectDatabase("/storage_test", VirtualFileSystem.ApplicationDatabaseIndexName);
            }

            public void Dispose()
            {
                temporaryDirectory.Dispose();
            }
        }
    }
}
#endif
