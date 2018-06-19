// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using NUnit.Framework;
using Xenko.Core.IO;
// ReSharper disable ObjectCreationAsStatement

namespace Xenko.Core.Design.Tests
{
    [TestFixture]
    public class TestUPath
    {
        [Test]
        public void TestUFileConstructor()
        {
            Assert.DoesNotThrow(() => new UFile(null));
            Assert.DoesNotThrow(() => new UFile(""));
            Assert.DoesNotThrow(() => { var s = "a"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = ".txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b/c/d.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b/c/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b/c/d.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b/c/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b/c/d.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b/c/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); });
            Assert.Throws<ArgumentException>(() => new UFile("a\""));
            Assert.Throws<ArgumentException>(() => new UFile("*.txt"));
            Assert.Throws<ArgumentException>(() => new UFile("/a/"));
            Assert.Throws<ArgumentException>(() => new UFile("/"));
            Assert.Throws<ArgumentException>(() => new UFile("E:/"));
            Assert.Throws<ArgumentException>(() => new UFile("E:"));
            Assert.Throws<ArgumentException>(() => new UFile("E:e"));
        }

        [Test]
        public void TestUDirectoryConstructor()
        {
            Assert.DoesNotThrow(() => new UDirectory(null));
            Assert.DoesNotThrow(() => new UDirectory(""));
            Assert.DoesNotThrow(() => { var s = "a"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a.txt/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = ".txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b/c/d.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "a/b/c/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b/c/d.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/b/c/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a.txt/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b/c/d.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/a/b/c/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "E:"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.DoesNotThrow(() => { var s = "/a/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); });
            Assert.Throws<ArgumentException>(() => new UDirectory("*.txt"));
            Assert.Throws<ArgumentException>(() => new UDirectory("E:e"));
        }

        [Test]
        public void TestUPathFullPath()
        {
            Assert.AreEqual("a", new UDirectory("a").FullPath);
            Assert.AreEqual("/a", new UDirectory("/a").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b").FullPath);
            Assert.AreEqual("/b/c", new UDirectory("/b/c").FullPath);
            Assert.AreEqual("ab/c", new UDirectory("ab/c").FullPath);
            Assert.AreEqual("/ab/c", new UDirectory("/ab/c").FullPath);
            Assert.AreEqual("c:/", new UDirectory("c:/").FullPath);
            Assert.AreEqual("c:/a", new UDirectory("c:/a").FullPath);

            // TODO (include tests with parent and self paths .. and .)
        }

        [Test]
        public void TestUPathHasDrive()
        {
            // TODO
        }

        [Test]
        public void TestUPathHasDirectory()
        {
            Assert.True(new UFile("/a/b.txt").HasDirectory);
            Assert.True(new UFile("/a/b/c.txt").HasDirectory);
            Assert.True(new UFile("/a/b/c").HasDirectory);
            Assert.True(new UFile("/a.txt").HasDirectory);
            Assert.True(new UFile("E:/a.txt").HasDirectory);
            Assert.True(new UFile("E:/a/b.txt").HasDirectory);
            Assert.True(new UFile("E:/a/b/c.txt").HasDirectory);
            Assert.True(new UFile("E:/a/b/c").HasDirectory);
            Assert.True(new UDirectory("/a/b/c").HasDirectory);
            Assert.True(new UDirectory("E:/a/b/c").HasDirectory);
            Assert.True(new UDirectory("/a").HasDirectory);
            Assert.True(new UDirectory("E:/a").HasDirectory);
            Assert.True(new UDirectory("/").HasDirectory);
            Assert.True(new UDirectory("E:/").HasDirectory);
            Assert.True(new UDirectory("E:").HasDirectory);
            Assert.False(new UFile("a.txt").HasDirectory);
            Assert.False(new UFile("a").HasDirectory);
        }

        [Test]
        public void TestUPathIsRelativeAndIsAbsolute()
        {
            var assert = new Action<UPath, bool>((x, isAbsolute) =>
            {
                Assert.AreEqual(isAbsolute, x.IsAbsolute);
                Assert.AreEqual(!isAbsolute, x.IsRelative);
            });
            assert(new UFile("/a/b/c.txt"), true);
            assert(new UFile("E:/a/b/c.txt"), true);
            assert(new UDirectory("/c.txt"), true);
            assert(new UDirectory("/"), true);
            assert(new UFile("a/b/c.txt"), false);
            assert(new UFile("../c.txt"), false);
        }

        [Test]
        public void TestUPathIsFile()
        {
            // TODO
        }

        [Test]
        public void TestUPathPathType()
        {
            // TODO
        }

        [Test]
        public void TestUPathIsNullOrEmpty()
        {
            Assert.True(UPath.IsNullOrEmpty(new UFile(null)));
            Assert.True(UPath.IsNullOrEmpty(new UFile("")));
            Assert.True(UPath.IsNullOrEmpty(new UFile(" ")));
            Assert.True(UPath.IsNullOrEmpty(new UDirectory(null)));
            Assert.True(UPath.IsNullOrEmpty(new UDirectory("")));
            Assert.True(UPath.IsNullOrEmpty(new UDirectory(" ")));
            Assert.True(UPath.IsNullOrEmpty(null));
            Assert.False(UPath.IsNullOrEmpty(new UFile("a")));
            Assert.False(UPath.IsNullOrEmpty(new UDirectory("a")));
            Assert.False(UPath.IsNullOrEmpty(new UDirectory("C:/")));
            Assert.False(UPath.IsNullOrEmpty(new UDirectory("/")));
        }

        [Test]
        public void TestUPathGetDrive()
        {
            // TODO
        }

        [Test]
        [Obsolete("Test GetFullDirectory instead")]
        public void TestUPathGetDirectory()
        {
            Assert.AreEqual("/", new UDirectory("/").GetDirectory());
            Assert.AreEqual("a", new UDirectory("a").GetDirectory());
            Assert.AreEqual("/a", new UDirectory("/a").GetDirectory());
            Assert.AreEqual("a/b", new UDirectory("a/b").GetDirectory());
            Assert.AreEqual("/b/c", new UDirectory("/b/c").GetDirectory());
            Assert.AreEqual("ab/c", new UDirectory("ab/c").GetDirectory());
            Assert.AreEqual("/ab/c", new UDirectory("/ab/c").GetDirectory());
            Assert.AreEqual("/a/b/c", new UDirectory("/a/b/c").GetDirectory());
            Assert.AreEqual("/", new UDirectory("c:").GetDirectory());
            Assert.AreEqual("/", new UDirectory("c:/").GetDirectory());
            Assert.AreEqual("/a", new UDirectory("c:/a").GetDirectory());
            Assert.AreEqual("/a/b", new UDirectory("c:/a/b").GetDirectory());
            Assert.AreEqual("/", new UFile("/a.txt").GetDirectory());
            Assert.AreEqual("/", new UFile("c:/a.txt").GetDirectory());
            // TODO
        }

        [Test]
        public void TestUPathGetParent()
        {
            // First directories
            var dir = new UDirectory("c:/");
            Assert.AreEqual("c:/", dir.GetParent().FullPath);

            dir = new UDirectory("c:/a");
            Assert.AreEqual("c:/", dir.GetParent().FullPath);

            dir = new UDirectory("c:/a/b");
            Assert.AreEqual("c:/a", dir.GetParent().FullPath);

            dir = new UDirectory("/");
            Assert.AreEqual("/", dir.GetParent().FullPath);

            dir = new UDirectory("/a");
            Assert.AreEqual("/", dir.GetParent().FullPath);

            dir = new UDirectory("/a/b");
            Assert.AreEqual("/a", dir.GetParent().FullPath);

            dir = new UDirectory("a");
            Assert.AreEqual("", dir.GetParent().FullPath);

            dir = new UDirectory("a/b");
            Assert.AreEqual("a", dir.GetParent().FullPath);

            // Now with files.
            var file = new UFile("c:/a.txt");
            Assert.AreEqual("c:/", file.GetParent().FullPath);

            file = new UFile("c:/a/b.txt");
            Assert.AreEqual("c:/a", file.GetParent().FullPath);

            file = new UFile("/a.txt");
            Assert.AreEqual("/", file.GetParent().FullPath);

            file = new UFile("/a/b.txt");
            Assert.AreEqual("/a", file.GetParent().FullPath);

            file = new UFile("a.txt");
            Assert.AreEqual("", file.GetParent().FullPath);

            file = new UFile("a/b.txt");
            Assert.AreEqual("a", file.GetParent().FullPath);
        }

        [Test]
        public void TestUPathGetFullDirectory()
        {
            Assert.AreEqual(new UDirectory("/a"), new UFile("/a/b.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory("/a/b"), new UFile("/a/b/c.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory("/a/b"), new UFile("/a/b/c").GetFullDirectory());
            Assert.AreEqual(new UDirectory("/"), new UFile("/a.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/"), new UFile("E:/a.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/a"), new UFile("E:/a/b.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/a/b"), new UFile("E:/a/b/c.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/a/b"), new UFile("E:/a/b/c").GetFullDirectory());
            Assert.AreEqual(new UDirectory("/a/b/c"), new UDirectory("/a/b/c").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/a/b/c"), new UDirectory("E:/a/b/c").GetFullDirectory());
            Assert.AreEqual(new UDirectory("/a"), new UDirectory("/a").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/a"), new UDirectory("E:/a").GetFullDirectory());
            Assert.AreEqual(new UDirectory("/"), new UDirectory("/").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/"), new UDirectory("E:/").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:/"), new UDirectory("E:").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:"), new UDirectory("E:/").GetFullDirectory());
            Assert.AreEqual(new UDirectory("E:"), new UDirectory("E:").GetFullDirectory());
            Assert.AreEqual(new UDirectory(null), new UFile("a.txt").GetFullDirectory());
            Assert.AreEqual(new UDirectory(null), new UFile("").GetFullDirectory());

            Assert.AreEqual("a", new UDirectory("a").GetFullDirectory().FullPath);
            Assert.AreEqual("/a", new UDirectory("/a").GetFullDirectory().FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b").GetFullDirectory().FullPath);
            Assert.AreEqual("/b/c", new UDirectory("/b/c").GetFullDirectory().FullPath);
            Assert.AreEqual("ab/c", new UDirectory("ab/c").GetFullDirectory().FullPath);
            Assert.AreEqual("/ab/c", new UDirectory("/ab/c").GetFullDirectory().FullPath);
            Assert.AreEqual("E:/", new UDirectory("E:/").GetFullDirectory().FullPath);
            Assert.AreEqual("E:/", new UDirectory("E:").GetFullDirectory().FullPath);
            Assert.AreEqual("E:/a", new UDirectory("E:/a").GetFullDirectory().FullPath);
        }

        //[Test]
        public void TestUPathGetComponents()
        {
            var d = new UDirectory("a/b");
            Assert.AreEqual(new UDirectory("").GetComponents().Count(), 0);
            Assert.AreEqual(new UDirectory("/").GetComponents().Count(), 0);
            CollectionAssert.AreEqual(new UDirectory("a").GetComponents(), new string[] { "a" });
            CollectionAssert.AreEqual(new UDirectory("/a").GetComponents(), new string[] { "a" });
            CollectionAssert.AreEqual(new UDirectory("a/b").GetComponents(), new string[] { "a", "b" });
            CollectionAssert.AreEqual(new UDirectory("/a/b").GetComponents(), new string[] { "a", "b" });
            CollectionAssert.AreEqual(new UDirectory("a/b/c").GetComponents(), new string[] { "a", "b", "c" });
            CollectionAssert.AreEqual(new UDirectory("/a/b/c").GetComponents(), new string[] { "a", "b", "c" });
            CollectionAssert.AreEqual(new UDirectory("c:").GetComponents(), new string[] { "c:" });
            CollectionAssert.AreEqual(new UDirectory("c:/a").GetComponents(), new string[] { "c:", "a" });
            CollectionAssert.AreEqual(new UDirectory("c:/a/b").GetComponents(), new string[] { "c:", "a", "b" });
            CollectionAssert.AreEqual(new UDirectory("c:/a/b.ext").GetComponents(), new string[] { "c:", "a", "b.ext" });


            CollectionAssert.AreEqual(new UFile("a").GetComponents(), new string[] { "a" });
            CollectionAssert.AreEqual(new UFile("/a").GetComponents(), new string[] { "a" });
            CollectionAssert.AreEqual(new UFile("a/b").GetComponents(), new string[] { "a", "b" });
            CollectionAssert.AreEqual(new UFile("/a/b").GetComponents(), new string[] { "a", "b" });
            CollectionAssert.AreEqual(new UFile("a/b/c").GetComponents(), new string[] { "a", "b", "c" });
            CollectionAssert.AreEqual(new UFile("/a/b/c").GetComponents(), new string[] { "a", "b", "c" });
            CollectionAssert.AreEqual(new UFile("c:/a").GetComponents(), new string[] { "c:", "a" });
            CollectionAssert.AreEqual(new UFile("c:/a/b").GetComponents(), new string[] { "c:", "a", "b" });
            CollectionAssert.AreEqual(new UFile("c:/a/b.ext").GetComponents(), new string[] { "c:", "a", "b.ext" });
        }

        [Test]
        public void TestUPathEquals()
        {
            // TODO
        }

        [Test]
        public void TestUPathGetHashCode()
        {
            // TODO
        }

        [Test]
        public void TestUPathCompare()
        {
            // TODO
        }

        [Test]
        public void TestUPathToString()
        {
            // TODO
        }

        [Test]
        public void TestUPathToWindowsPath()
        {
            // TODO
        }

        [Test]
        public void TestUPathCombine()
        {
            // TODO: not enough test!
            Assert.AreEqual(new UFile("e.txt"), UPath.Combine(".", new UFile("e.txt")));
            Assert.AreEqual(new UFile("/a/b/d/e.txt"), UPath.Combine("/a/b/c", new UFile("../d/e.txt")));
            Assert.AreEqual(new UFile("/d/e.txt"), UPath.Combine("/a/b/c", new UFile("../../../d/e.txt")));
            Assert.AreEqual(new UFile("/d/e.txt"), UPath.Combine("/a/b/c", new UFile("../../../../../../d/e.txt")));
            Assert.AreEqual(new UFile("C:/a/d/e.txt"), UPath.Combine("C:/a/b/c", new UFile("../../d/e.txt")));
            Assert.AreEqual(new UFile("C:/d/e.txt"), UPath.Combine("C:/a/b/c", new UFile("../../../d/e.txt")));
            Assert.AreEqual(new UFile("C:/d/e.txt"), UPath.Combine("C:/a/b/c", new UFile("../../../../../../d/e.txt")));
            Assert.AreEqual(new UFile("C:/a.txt"), UPath.Combine("C:/", new UFile("a.txt")));
            Assert.AreEqual(new UFile("C:/a/b.txt"), UPath.Combine("C:/a", new UFile("b.txt")));
            Assert.AreEqual(new UFile("C:/a.txt"), UPath.Combine("C:/", new UFile("./a.txt")));
            Assert.AreEqual(new UFile("C:/a/b.txt"), UPath.Combine("C:/a", new UFile("./b.txt")));
            Assert.AreEqual(new UFile("C:/a.txt"), UPath.Combine("C:/", new UFile("././a.txt")));
            Assert.AreEqual(new UFile("C:/a/b.txt"), UPath.Combine("C:/a", new UFile("././b.txt")));
        }

        [Test]
        public void TestUPathMakeRelative()
        {
            // TODO
            //Assert.AreEqual(new UDirectory("../.."), new UDirectory("C:/a").MakeRelative("/a/b/c"));
        }

        [Test]
        public void TestUPathHasDirectoryChars()
        {
            // TODO
        }

        [Test]
        public void TestUPathIsValid()
        {
            // TODO
        }

        [Test]
        public void TestUPathNormalize()
        {
            // TODO - maybe we should turn this method private? Or keep a single overload public?
            Assert.AreEqual("test.txt", new UDirectory("test.txt").FullPath);
            Assert.AreEqual("a", new UDirectory("a").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b").FullPath);

            // Test '..'
            Assert.AreEqual("../a", new UDirectory("../a").FullPath);
            Assert.AreEqual("../a/b/c", new UDirectory("../a/b/c").FullPath);
            Assert.AreEqual("../../a", new UDirectory("../../a").FullPath);
            Assert.AreEqual("b/c", new UDirectory("a/../b/c").FullPath);
            Assert.AreEqual("../b/c", new UDirectory("a/../../b/c").FullPath);
            Assert.AreEqual("a/c", new UDirectory("a/b/../c").FullPath);
            Assert.AreEqual("../c", new UDirectory("a/../../c").FullPath);
            Assert.AreEqual("..", new UDirectory("a/../../c/..").FullPath);
            Assert.AreEqual("../..", new UDirectory("a/../../c/../..").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b/c/..").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b/c/../").FullPath);

            // Test '.'
            Assert.AreEqual(".", new UDirectory(".").FullPath);
            Assert.AreEqual(".", new UDirectory("././.").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/././b").FullPath);
            Assert.AreEqual("a/b", new UDirectory("././a/b").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b/./.").FullPath);
            Assert.AreEqual("a/b", new UDirectory("a/b/././").FullPath);

            // Test duplicate '/'
            Assert.AreEqual("a/b/c", new UDirectory("a///b/c").FullPath);
            Assert.AreEqual("a/b/c", new UDirectory("a///b/c/").FullPath);
            Assert.AreEqual("a/b/c", new UDirectory("a/b/c/////").FullPath);
            Assert.AreEqual("/a/b/c", new UDirectory("////a/b/c/").FullPath);
            Assert.AreEqual("/", new UDirectory("/////").FullPath);

            // Test '\'
            Assert.AreEqual("a/b/c", new UDirectory(@"a\b\c").FullPath);

            // Test rooted path
            Assert.AreEqual("/a/b", new UDirectory("/a/b").FullPath);

            // Test drive
            Assert.AreEqual("C:/a/b/c", new UDirectory("C:/a/b/c").FullPath);
            Assert.AreEqual("C:/", new UDirectory("C:/..").FullPath);
            Assert.AreEqual("C:/", new UDirectory("C:/../").FullPath);
            Assert.AreEqual("C:/", new UDirectory("C:/../..").FullPath);
            Assert.AreEqual("C:/", new UDirectory("C:/../../").FullPath);


            Assert.AreEqual("/", new UDirectory("/..").FullPath);
            Assert.AreEqual("..", new UDirectory("..").FullPath);
            Assert.AreEqual("E:/", new UDirectory("E:/..").FullPath);
            Assert.AreEqual("..", new UDirectory("..").FullPath);
            Assert.AreEqual("/a", new UDirectory("/a/").FullPath);
            Assert.AreEqual("../../c.txt", new UFile("a/../../../c.txt").FullPath);
        }

        [Test]
        public void TestUFileGetDirectoryAndFileName()
        {
            // TODO
        }

        [Test]
        public void TestUFileGetFileName()
        {
            // TODO
        }

        [Test]
        public void TestUFileGetFileExtension()
        {
            // TODO
        }

        [Test]
        public void TestUFileGetFileNameWithExtension()
        {
            // TODO
        }

        [Test]
        public void TestUFileGetFullPathWithoutExtension()
        {
            // TODO
        }

        [Test]
        public void TestUFileIsValid()
        {
            // TODO
        }

        [Test]
        public void TestUDirectoryContains()
        {
            // TODO
        }

        [Test]
        public void TestUDirectoryGetDirectoryName()
        {
            // TODO
        }
    }
}
