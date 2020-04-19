// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xunit;
using Stride.Core.IO;
// ReSharper disable ObjectCreationAsStatement

namespace Stride.Core.Design.Tests
{
    public class TestUPath
    {
        [Fact]
        public void TestUFileConstructor()
        {
            new UFile(null);
            new UFile("");
            { var s = "a"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "a.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = ".txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "/a"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "/a.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "a/b"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "a/b.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "a/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "a/b/c/d.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "a/b/c/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "/a/b"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "/a/b.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "/a/b/c/d.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "/a/b/c/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "E:/a.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "E:/a/b"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "E:/a/b.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "E:/a/b/c/d.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            { var s = "E:/a/b/c/.txt"; new UFile(s); new UFile(s.Replace('/', '\\')); }
            Assert.Throws<ArgumentException>(() => new UFile("a\""));
            Assert.Throws<ArgumentException>(() => new UFile("*.txt"));
            Assert.Throws<ArgumentException>(() => new UFile("/a/"));
            Assert.Throws<ArgumentException>(() => new UFile("/"));
            Assert.Throws<ArgumentException>(() => new UFile("E:/"));
            Assert.Throws<ArgumentException>(() => new UFile("E:"));
            Assert.Throws<ArgumentException>(() => new UFile("E:e"));
        }

        [Fact]
        public void TestUDirectoryConstructor()
        {
            new UDirectory(null);
            new UDirectory("");
            { var s = "a"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a.txt/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = ".txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/b"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/b/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/b.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/b/c/d.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "a/b/c/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a/b"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a/b.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a/b/c/d.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a/b/c/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/a.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/a.txt/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/a/b"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/a/b.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/a/b/c/d.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/a/b/c/.txt"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "E:"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            { var s = "/a/"; new UDirectory(s); new UDirectory(s.Replace('/', '\\')); }
            Assert.Throws<ArgumentException>(() => new UDirectory("*.txt"));
            Assert.Throws<ArgumentException>(() => new UDirectory("E:e"));
        }

        [Fact]
        public void TestUPathFullPath()
        {
            Assert.Equal("a", new UDirectory("a").FullPath);
            Assert.Equal("/a", new UDirectory("/a").FullPath);
            Assert.Equal("a/b", new UDirectory("a/b").FullPath);
            Assert.Equal("/b/c", new UDirectory("/b/c").FullPath);
            Assert.Equal("ab/c", new UDirectory("ab/c").FullPath);
            Assert.Equal("/ab/c", new UDirectory("/ab/c").FullPath);
            Assert.Equal("c:/", new UDirectory("c:/").FullPath);
            Assert.Equal("c:/a", new UDirectory("c:/a").FullPath);

            // TODO (include tests with parent and self paths .. and .)
        }

        [Fact]
        public void TestUPathHasDrive()
        {
            // TODO
        }

        [Fact]
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

        [Fact]
        public void TestUPathIsRelativeAndIsAbsolute()
        {
            var assert = new Action<UPath, bool>((x, isAbsolute) =>
            {
                Assert.Equal(isAbsolute, x.IsAbsolute);
                Assert.Equal(!isAbsolute, x.IsRelative);
            });
            assert(new UFile("/a/b/c.txt"), true);
            assert(new UFile("E:/a/b/c.txt"), true);
            assert(new UDirectory("/c.txt"), true);
            assert(new UDirectory("/"), true);
            assert(new UFile("a/b/c.txt"), false);
            assert(new UFile("../c.txt"), false);
        }

        [Fact]
        public void TestUPathIsFile()
        {
            // TODO
        }

        [Fact]
        public void TestUPathPathType()
        {
            // TODO
        }

        [Fact]
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

        [Fact]
        public void TestUPathGetDrive()
        {
            // TODO
        }

        [Fact]
        [Obsolete("Test GetFullDirectory instead")]
        public void TestUPathGetDirectory()
        {
            Assert.Equal("/", new UDirectory("/").GetDirectory());
            Assert.Equal("a", new UDirectory("a").GetDirectory());
            Assert.Equal("/a", new UDirectory("/a").GetDirectory());
            Assert.Equal("a/b", new UDirectory("a/b").GetDirectory());
            Assert.Equal("/b/c", new UDirectory("/b/c").GetDirectory());
            Assert.Equal("ab/c", new UDirectory("ab/c").GetDirectory());
            Assert.Equal("/ab/c", new UDirectory("/ab/c").GetDirectory());
            Assert.Equal("/a/b/c", new UDirectory("/a/b/c").GetDirectory());
            Assert.Equal("/", new UDirectory("c:").GetDirectory());
            Assert.Equal("/", new UDirectory("c:/").GetDirectory());
            Assert.Equal("/a", new UDirectory("c:/a").GetDirectory());
            Assert.Equal("/a/b", new UDirectory("c:/a/b").GetDirectory());
            Assert.Equal("/", new UFile("/a.txt").GetDirectory());
            Assert.Equal("/", new UFile("c:/a.txt").GetDirectory());
            // TODO
        }

        [Fact]
        public void TestUPathGetParent()
        {
            // First directories
            var dir = new UDirectory("c:/");
            Assert.Equal("c:/", dir.GetParent().FullPath);

            dir = new UDirectory("c:/a");
            Assert.Equal("c:/", dir.GetParent().FullPath);

            dir = new UDirectory("c:/a/b");
            Assert.Equal("c:/a", dir.GetParent().FullPath);

            dir = new UDirectory("/");
            Assert.Equal("/", dir.GetParent().FullPath);

            dir = new UDirectory("/a");
            Assert.Equal("/", dir.GetParent().FullPath);

            dir = new UDirectory("/a/b");
            Assert.Equal("/a", dir.GetParent().FullPath);

            dir = new UDirectory("a");
            Assert.Equal("", dir.GetParent().FullPath);

            dir = new UDirectory("a/b");
            Assert.Equal("a", dir.GetParent().FullPath);

            // Now with files.
            var file = new UFile("c:/a.txt");
            Assert.Equal("c:/", file.GetParent().FullPath);

            file = new UFile("c:/a/b.txt");
            Assert.Equal("c:/a", file.GetParent().FullPath);

            file = new UFile("/a.txt");
            Assert.Equal("/", file.GetParent().FullPath);

            file = new UFile("/a/b.txt");
            Assert.Equal("/a", file.GetParent().FullPath);

            file = new UFile("a.txt");
            Assert.Equal("", file.GetParent().FullPath);

            file = new UFile("a/b.txt");
            Assert.Equal("a", file.GetParent().FullPath);
        }

        [Fact]
        public void TestUPathGetFullDirectory()
        {
            Assert.Equal(new UDirectory("/a"), new UFile("/a/b.txt").GetFullDirectory());
            Assert.Equal(new UDirectory("/a/b"), new UFile("/a/b/c.txt").GetFullDirectory());
            Assert.Equal(new UDirectory("/a/b"), new UFile("/a/b/c").GetFullDirectory());
            Assert.Equal(new UDirectory("/"), new UFile("/a.txt").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/"), new UFile("E:/a.txt").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/a"), new UFile("E:/a/b.txt").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/a/b"), new UFile("E:/a/b/c.txt").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/a/b"), new UFile("E:/a/b/c").GetFullDirectory());
            Assert.Equal(new UDirectory("/a/b/c"), new UDirectory("/a/b/c").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/a/b/c"), new UDirectory("E:/a/b/c").GetFullDirectory());
            Assert.Equal(new UDirectory("/a"), new UDirectory("/a").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/a"), new UDirectory("E:/a").GetFullDirectory());
            Assert.Equal(new UDirectory("/"), new UDirectory("/").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/"), new UDirectory("E:/").GetFullDirectory());
            Assert.Equal(new UDirectory("E:/"), new UDirectory("E:").GetFullDirectory());
            Assert.Equal(new UDirectory("E:"), new UDirectory("E:/").GetFullDirectory());
            Assert.Equal(new UDirectory("E:"), new UDirectory("E:").GetFullDirectory());
            Assert.Equal(new UDirectory(null), new UFile("a.txt").GetFullDirectory());
            Assert.Equal(new UDirectory(null), new UFile("").GetFullDirectory());

            Assert.Equal("a", new UDirectory("a").GetFullDirectory().FullPath);
            Assert.Equal("/a", new UDirectory("/a").GetFullDirectory().FullPath);
            Assert.Equal("a/b", new UDirectory("a/b").GetFullDirectory().FullPath);
            Assert.Equal("/b/c", new UDirectory("/b/c").GetFullDirectory().FullPath);
            Assert.Equal("ab/c", new UDirectory("ab/c").GetFullDirectory().FullPath);
            Assert.Equal("/ab/c", new UDirectory("/ab/c").GetFullDirectory().FullPath);
            Assert.Equal("E:/", new UDirectory("E:/").GetFullDirectory().FullPath);
            Assert.Equal("E:/", new UDirectory("E:").GetFullDirectory().FullPath);
            Assert.Equal("E:/a", new UDirectory("E:/a").GetFullDirectory().FullPath);
        }

        [Fact]
        public void TestUPathGetComponents()
        {
            var d = new UDirectory("a/b");
            Assert.Empty(new UDirectory("").GetComponents());
            Assert.Empty(new UDirectory("/").GetComponents());
            Assert.Equal(new UDirectory("a").GetComponents(), new[] { "a" });
            Assert.Equal(new UDirectory("/a").GetComponents(), new[] { "a" });
            Assert.Equal(new UDirectory("a/b").GetComponents(), new[] { "a", "b" });
            Assert.Equal(new UDirectory("/a/b").GetComponents(), new[] { "a", "b" });
            Assert.Equal(new UDirectory("a/b/c").GetComponents(), new[] { "a", "b", "c" });
            Assert.Equal(new UDirectory("/a/b/c").GetComponents(), new[] { "a", "b", "c" });
            Assert.Equal(new UDirectory("c:").GetComponents(), new[] { "c:" });
            Assert.Equal(new UDirectory("c:/a").GetComponents(), new[] { "c:", "a" });
            Assert.Equal(new UDirectory("c:/a/b").GetComponents(), new[] { "c:", "a", "b" });
            Assert.Equal(new UDirectory("c:/a/b.ext").GetComponents(), new[] { "c:", "a", "b.ext" });


            Assert.Equal(new UFile("a").GetComponents(), new[] { "a" });
            Assert.Equal(new UFile("/a").GetComponents(), new[] { "a" });
            Assert.Equal(new UFile("a/b").GetComponents(), new[] { "a", "b" });
            Assert.Equal(new UFile("/a/b").GetComponents(), new[] { "a", "b" });
            Assert.Equal(new UFile("a/b/c").GetComponents(), new[] { "a", "b", "c" });
            Assert.Equal(new UFile("/a/b/c").GetComponents(), new[] { "a", "b", "c" });
            Assert.Equal(new UFile("c:/a").GetComponents(), new[] { "c:", "a" });
            Assert.Equal(new UFile("c:/a/b").GetComponents(), new[] { "c:", "a", "b" });
            Assert.Equal(new UFile("c:/a/b.ext").GetComponents(), new[] { "c:", "a", "b.ext" });
        }

        [Fact]
        public void TestUPathEquals()
        {
            // TODO
        }

        [Fact]
        public void TestUPathGetHashCode()
        {
            // TODO
        }

        [Fact]
        public void TestUPathCompare()
        {
            // TODO
        }

        [Fact]
        public void TestUPathToString()
        {
            // TODO
        }

        [Fact]
        public void TestUPathToWindowsPath()
        {
            // TODO
        }

        [Fact]
        public void TestUPathCombine()
        {
            // TODO: not enough test!
            Assert.Equal(new UFile("e.txt"), UPath.Combine(".", new UFile("e.txt")));
            Assert.Equal(new UFile("/a/b/d/e.txt"), UPath.Combine("/a/b/c", new UFile("../d/e.txt")));
            Assert.Equal(new UFile("/d/e.txt"), UPath.Combine("/a/b/c", new UFile("../../../d/e.txt")));
            Assert.Equal(new UFile("/d/e.txt"), UPath.Combine("/a/b/c", new UFile("../../../../../../d/e.txt")));
            Assert.Equal(new UFile("C:/a/d/e.txt"), UPath.Combine("C:/a/b/c", new UFile("../../d/e.txt")));
            Assert.Equal(new UFile("C:/d/e.txt"), UPath.Combine("C:/a/b/c", new UFile("../../../d/e.txt")));
            Assert.Equal(new UFile("C:/d/e.txt"), UPath.Combine("C:/a/b/c", new UFile("../../../../../../d/e.txt")));
            Assert.Equal(new UFile("C:/a.txt"), UPath.Combine("C:/", new UFile("a.txt")));
            Assert.Equal(new UFile("C:/a/b.txt"), UPath.Combine("C:/a", new UFile("b.txt")));
            Assert.Equal(new UFile("C:/a.txt"), UPath.Combine("C:/", new UFile("./a.txt")));
            Assert.Equal(new UFile("C:/a/b.txt"), UPath.Combine("C:/a", new UFile("./b.txt")));
            Assert.Equal(new UFile("C:/a.txt"), UPath.Combine("C:/", new UFile("././a.txt")));
            Assert.Equal(new UFile("C:/a/b.txt"), UPath.Combine("C:/a", new UFile("././b.txt")));
        }

        [Fact]
        public void TestUPathMakeRelative()
        {
            // TODO
            //Assert.Equal(new UDirectory("../.."), new UDirectory("C:/a").MakeRelative("/a/b/c"));
        }

        [Fact]
        public void TestUPathHasDirectoryChars()
        {
            // TODO
        }

        [Fact]
        public void TestUPathIsValid()
        {
            // TODO
        }

        [Fact]
        public void TestUPathNormalize()
        {
            // TODO - maybe we should turn this method private? Or keep a single overload public?
            Assert.Equal("test.txt", new UDirectory("test.txt").FullPath);
            Assert.Equal("a", new UDirectory("a").FullPath);
            Assert.Equal("a/b", new UDirectory("a/b").FullPath);

            // Test '..'
            Assert.Equal("../a", new UDirectory("../a").FullPath);
            Assert.Equal("../a/b/c", new UDirectory("../a/b/c").FullPath);
            Assert.Equal("../../a", new UDirectory("../../a").FullPath);
            Assert.Equal("b/c", new UDirectory("a/../b/c").FullPath);
            Assert.Equal("../b/c", new UDirectory("a/../../b/c").FullPath);
            Assert.Equal("a/c", new UDirectory("a/b/../c").FullPath);
            Assert.Equal("../c", new UDirectory("a/../../c").FullPath);
            Assert.Equal("..", new UDirectory("a/../../c/..").FullPath);
            Assert.Equal("../..", new UDirectory("a/../../c/../..").FullPath);
            Assert.Equal("a/b", new UDirectory("a/b/c/..").FullPath);
            Assert.Equal("a/b", new UDirectory("a/b/c/../").FullPath);

            // Test '.'
            Assert.Equal(".", new UDirectory(".").FullPath);
            Assert.Equal(".", new UDirectory("././.").FullPath);
            Assert.Equal("a/b", new UDirectory("a/././b").FullPath);
            Assert.Equal("a/b", new UDirectory("././a/b").FullPath);
            Assert.Equal("a/b", new UDirectory("a/b/./.").FullPath);
            Assert.Equal("a/b", new UDirectory("a/b/././").FullPath);

            // Test duplicate '/'
            Assert.Equal("a/b/c", new UDirectory("a///b/c").FullPath);
            Assert.Equal("a/b/c", new UDirectory("a///b/c/").FullPath);
            Assert.Equal("a/b/c", new UDirectory("a/b/c/////").FullPath);
            Assert.Equal("/a/b/c", new UDirectory("////a/b/c/").FullPath);
            Assert.Equal("/", new UDirectory("/////").FullPath);

            // Test '\'
            Assert.Equal("a/b/c", new UDirectory(@"a\b\c").FullPath);

            // Test rooted path
            Assert.Equal("/a/b", new UDirectory("/a/b").FullPath);

            // Test drive
            Assert.Equal("C:/a/b/c", new UDirectory("C:/a/b/c").FullPath);
            Assert.Equal("C:/", new UDirectory("C:/..").FullPath);
            Assert.Equal("C:/", new UDirectory("C:/../").FullPath);
            Assert.Equal("C:/", new UDirectory("C:/../..").FullPath);
            Assert.Equal("C:/", new UDirectory("C:/../../").FullPath);


            Assert.Equal("/", new UDirectory("/..").FullPath);
            Assert.Equal("..", new UDirectory("..").FullPath);
            Assert.Equal("E:/", new UDirectory("E:/..").FullPath);
            Assert.Equal("..", new UDirectory("..").FullPath);
            Assert.Equal("/a", new UDirectory("/a/").FullPath);
            Assert.Equal("../../c.txt", new UFile("a/../../../c.txt").FullPath);
        }

        [Fact]
        public void TestUFileGetDirectoryAndFileName()
        {
            // TODO
        }

        [Fact]
        public void TestUFileGetFileName()
        {
            // TODO
        }

        [Fact]
        public void TestUFileGetFileExtension()
        {
            // TODO
        }

        [Fact]
        public void TestUFileGetFileNameWithExtension()
        {
            // TODO
        }

        [Fact]
        public void TestUFileGetFullPathWithoutExtension()
        {
            // TODO
        }

        [Fact]
        public void TestUFileIsValid()
        {
            // TODO
        }

        [Fact]
        public void TestUDirectoryContains()
        {
            // TODO
        }

        [Fact]
        public void TestUDirectoryGetDirectoryName()
        {
            // TODO
        }
    }
}
