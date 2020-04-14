// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.IO;

namespace Stride.Core.Design.Tests
{
    // TODO: Tests in this class should be migrated to the new testUPath class where we do one test method per UFile method/property
    public class TestUPathOld
    {
        [Fact]
        public void TestNormalize()
        {
            string error;

            StringSpan driveSpan;
            StringSpan dirSpan;
            StringSpan nameSpan;

            var text = UPath.Normalize("test.txt", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("test.txt", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 8), dirSpan);

            text = UPath.Normalize("a", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 1), dirSpan);

            text = UPath.Normalize("a/b", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test leading '..'
            text = UPath.Normalize("../a/b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("../a/b/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 7), dirSpan);
            Assert.Equal(new StringSpan(7, 1), nameSpan);

            // Test leading '..'
            text = UPath.Normalize("../a", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("../a", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 3), dirSpan);
            Assert.Equal(new StringSpan(3, 1), nameSpan);

            // Test leading '..'
            text = UPath.Normalize("../../a", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("../../a", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 6), dirSpan);
            Assert.Equal(new StringSpan(6, 1), nameSpan);

            // Test between '..'
            text = UPath.Normalize("a/../b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("b/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test between '..'
            text = UPath.Normalize("a/b/../c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test double '..'
            text = UPath.Normalize("a/../../c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("../c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 3), dirSpan);
            Assert.Equal(new StringSpan(3, 1), nameSpan);

            // Test double '..' and trailing '..'
            text = UPath.Normalize("a/../../c/..", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("..", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);

            // Test double '..' and trailing '..'
            text = UPath.Normalize("a/../../c/../..", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("../..", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 5), dirSpan);

            // Test trailing '..'
            text = UPath.Normalize("a/b/c/..", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test trailing '..' and trailing '/'
            text = UPath.Normalize("a/b/c/../", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test root '.'
            text = UPath.Normalize(".", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal(".", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test root '.'
            text = UPath.Normalize("././.", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal(".", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test root '.'
            text = UPath.Normalize("a/././b", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test leading '.'
            text = UPath.Normalize("././a/b", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test trailing '.'
            text = UPath.Normalize("a/b/./.", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);

            // Test trailing '.'
            text = UPath.Normalize("a/b/././", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), dirSpan);
            Assert.Equal(new StringSpan(2, 1), nameSpan);
            
            // Test duplicate '/'
            text = UPath.Normalize("a////b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 4), dirSpan);
            Assert.Equal(new StringSpan(4, 1), nameSpan);

            // Test backslash '\'
            text = UPath.Normalize(@"\a\b\c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("/a/b/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 5), dirSpan);
            Assert.Equal(new StringSpan(5, 1), nameSpan);

            // Test leading multiple '/'
            text = UPath.Normalize("////a/b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("/a/b/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 5), dirSpan);
            Assert.Equal(new StringSpan(5, 1), nameSpan);

            // Test Trailing multiple '/'
            text = UPath.Normalize("a/b/c////", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("a/b/c", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 4), dirSpan);
            Assert.Equal(new StringSpan(4, 1), nameSpan);

            // Test multiple '/'
            text = UPath.Normalize("////", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("/", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 1), dirSpan);

            // Test rooted path '/a/b'
            text = UPath.Normalize("/a/b", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("/a/b", text.ToString());
            Assert.False(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 3), dirSpan);
            Assert.Equal(new StringSpan(3, 1), nameSpan);

            // Test drive standard
            text = UPath.Normalize("C:/a/b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.NotNull(text);
            Assert.Equal("C:/a/b/c", text.ToString());
            Assert.True(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.True(nameSpan.IsValid);
            Assert.Equal(new StringSpan(0, 2), driveSpan);
            Assert.Equal(new StringSpan(2, 5), dirSpan);
            Assert.Equal(new StringSpan(7, 1), nameSpan);

            // Test drive backslash invalid
            UPath.Normalize("C:..", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.NotNull(error);
            Assert.False(driveSpan.IsValid);
            Assert.False(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test drive backslash invalid
            UPath.Normalize("C:/..", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.True(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test drive backslash invalid
            UPath.Normalize("C:/../", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.True(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test drive backslash invalid
            UPath.Normalize("C:/../..", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.Null(error);
            Assert.True(driveSpan.IsValid);
            Assert.True(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test drive start ':' is invalid
            UPath.Normalize(":a/b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.NotNull(error);
            Assert.False(driveSpan.IsValid);
            Assert.False(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test drive in the middle ':' is invalid
            UPath.Normalize("a/c:a/b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.NotNull(error);
            Assert.False(driveSpan.IsValid);
            Assert.False(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);

            // Test drive multiple ':' is invalid
            UPath.Normalize("a:c:a/b/c", out driveSpan, out dirSpan, out nameSpan, out error);
            Assert.NotNull(error);
            Assert.False(driveSpan.IsValid);
            Assert.False(dirSpan.IsValid);
            Assert.False(nameSpan.IsValid);
        }

        [Fact]
        public void TestFileExtension()
        {
            Assert.Equal("test", new UFile("test.txt").GetFileNameWithoutExtension());
            Assert.Equal(".txt", new UFile("test.txt").GetFileExtension());
            Assert.Null(new UFile(".txt").GetFileNameWithoutExtension());

            Assert.Equal("test.another", new UFile("test.another.txt").GetFileNameWithoutExtension());
            Assert.Equal(".txt", new UFile("test.another.txt").GetFileExtension());

            Assert.Equal(".txt", new UFile(".txt").GetFileExtension());
            Assert.False(new UFile("test.txt").IsAbsolute);
        }

        [Fact]
        public void TestIsDirectoryOnly()
        {
            var dirPath = new UDirectory("/a/b/c");
            Assert.Equal("/a/b/c", dirPath.GetDirectory());

            var filePath = new UFile("/test.txt");
            Assert.Equal("/", filePath.GetDirectory());
            Assert.Equal("test.txt", filePath.GetFileName());
        }

        [Fact]
        public void TestWithSimpleDirectory()
        {
            var assetPath = new UDirectory("/a/b/c");
            Assert.Equal("/a/b/c", assetPath.GetDirectory());
            Assert.Equal("/a/b/c", assetPath.FullPath);
            var directory = new UDirectory("C:/a");
            Assert.Equal("/a", directory.GetDirectory());
            directory = new UDirectory("/a");
            Assert.Equal("/a", directory.GetDirectory());
        }

        [Fact]
        public void TestWithSimplePath()
        {
            var assetPath = new UFile("/a/b/c");
            Assert.Equal("/a/b", assetPath.GetDirectory());
            Assert.Equal("c", assetPath.GetFileNameWithoutExtension());
            Assert.Null(assetPath.GetFileExtension());
            Assert.Equal("/a/b/c", assetPath.GetDirectoryAndFileNameWithoutExtension());
            Assert.Equal("/a/b/c", assetPath.FullPath);
        }

        [Fact]
        public void TestWithSimplePathWithExtension()
        {
            var assetPath = new UFile("/a/b/c.txt");
            Assert.Equal("/a/b", assetPath.GetDirectory());
            Assert.Equal("c", assetPath.GetFileNameWithoutExtension());
            Assert.Equal(".txt", assetPath.GetFileExtension());
            Assert.Equal("/a/b/c", assetPath.GetDirectoryAndFileNameWithoutExtension());
            Assert.Equal("/a/b/c.txt", assetPath.FullPath);
        }

        [Fact]
        public void TestWithNormalization()
        {
            var assetPath = new UFile("/a/b/.././././//c.txt");
            Assert.Equal("/a", assetPath.GetDirectory());
            Assert.Equal("c", assetPath.GetFileNameWithoutExtension());
            Assert.Equal(".txt", assetPath.GetFileExtension());
            Assert.Equal("/a/c", assetPath.GetDirectoryAndFileNameWithoutExtension());
            Assert.Equal("/a/c.txt", assetPath.FullPath);

            assetPath = new UFile("../.././././//c.txt");
            Assert.Equal("../..", assetPath.GetDirectory());
            Assert.Equal("c", assetPath.GetFileNameWithoutExtension());
            Assert.Equal(".txt", assetPath.GetFileExtension());
            Assert.Equal("../../c", assetPath.GetDirectoryAndFileNameWithoutExtension());
            Assert.Equal("../../c.txt", assetPath.FullPath);

            assetPath = new UFile("a/../../../c.txt");
            Assert.Equal("../../c.txt", assetPath.FullPath);
        }

        [Fact]
        public void TestEquals()
        {
            var assetPath1 = new UFile(null);
            var assetPath2 = new UFile(null);
            Assert.Equal(assetPath1, assetPath2);

            assetPath1 = new UFile("/a/b/c.txt");
            assetPath2 = new UFile("/a/b/d/../c.txt");
            Assert.Equal(assetPath1, assetPath2);

            // Test is not done on Extensions
            assetPath1 = new UFile("/a/b/c.txt");
            assetPath2 = new UFile("/a/b/d/../c.png");
            Assert.NotEqual(assetPath1, assetPath2);
            Assert.Equal(assetPath1.GetDirectoryAndFileNameWithoutExtension(), assetPath2.GetDirectoryAndFileNameWithoutExtension());
        }

        [Fact]
        public void TestCombine()
        {
            var path = UPath.Combine("/a/b/c", new UFile("../d/e.txt"));
            Assert.Equal("/a/b/d/e.txt", path.ToString());
        }

        [Fact]
        public void TestMixedSlash()
        {
            var assetPath1 = new UFile("/a\\b/c\\d.txt");
            var assetPath2 = new UFile("/a/b/c/d.txt");
            Assert.Equal(assetPath1.ToString(), assetPath2.ToString());
        }

        [Fact]
        public void TestMakeRelative()
        {
            UPath assetPath2 = null;
            UPath newAssetPath2 = null;
            var dir1 = new UDirectory("/a/b/c");

            var assetDir2 = new UDirectory("/a/b/c");
            newAssetPath2 = dir1.MakeRelative(assetDir2);
            Assert.Equal(".", newAssetPath2.FullPath);

            var assetDir3 = new UDirectory("/a/b");
            newAssetPath2 = dir1.MakeRelative(assetDir3);
            Assert.Equal("c", newAssetPath2.FullPath);

            var assetDir4 = new UDirectory("/a/b/c/d");
            newAssetPath2 = dir1.MakeRelative(assetDir4);
            Assert.Equal("..", newAssetPath2.FullPath);

            // Test direct relative
            assetPath2 = new UFile("/a/b/c/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("test.txt", newAssetPath2.FullPath);

            // Test direct relative + subdir
            assetPath2 = new UFile("/a/b/c/test/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("test/test.txt", newAssetPath2.FullPath);

            // Test relative 1
            assetPath2 = new UFile("/a/b/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../test.txt", newAssetPath2.FullPath);

            // Test relative 2
            assetPath2 = new UFile("/a/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../../test.txt", newAssetPath2.FullPath);

            // Test relative 3
            assetPath2 = new UFile("/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../../../test.txt", newAssetPath2.FullPath);

            // Test already relative
            assetPath2 = new UFile("../test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../test.txt", newAssetPath2.FullPath);

            // Test only root path in common
            assetPath2 = new UFile("/e/f/g/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../../../e/f/g/test.txt", newAssetPath2.FullPath);

            // Test only root path in common with single file
            assetPath2 = new UFile("/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../../../test.txt", newAssetPath2.FullPath);
        }

        [Fact]
        public void TestMakeRelativeWithDrive()
        {
            UPath assetPath2 = null;
            UPath newAssetPath2 = null;
            var dir1 = new UDirectory("C:/a/b/c");

            // Test direct relative
            assetPath2 = new UFile("C:/a/b/c/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("test.txt", newAssetPath2.FullPath);

            // Test direct relative + subdir
            assetPath2 = new UFile("C:/a/b/c/test/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("test/test.txt", newAssetPath2.FullPath);

            // Test relative 1
            assetPath2 = new UFile("C:/a/b/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../test.txt", newAssetPath2.FullPath);

            // Test relative 2
            assetPath2 = new UFile("C:/a/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../../test.txt", newAssetPath2.FullPath);

            // Test relative 3
            assetPath2 = new UFile("C:/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../../../test.txt", newAssetPath2.FullPath);

            // Test already relative
            assetPath2 = new UFile("../test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("../test.txt", newAssetPath2.FullPath);

            // Test no path in common
            assetPath2 = new UFile("E:/e/f/g/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("E:/e/f/g/test.txt", newAssetPath2.FullPath);

            // Test no root path single file
            assetPath2 = new UFile("E:/test.txt");
            newAssetPath2 = assetPath2.MakeRelative(dir1);
            Assert.Equal("E:/test.txt", newAssetPath2.FullPath);
        }

        [Fact]
        public void TestContains()
        {
            var dir1 = new UDirectory("C:/a/b/c");
            Assert.True(dir1.Contains(new UFile("C:/a/b/c/d")));
            Assert.True(dir1.Contains(new UFile("C:/a/b/c/d/e")));
            Assert.True(dir1.Contains(new UDirectory("C:/a/b/c/d")));
            Assert.True(dir1.Contains(new UDirectory("C:/a/b/c/d/e")));
            Assert.False(dir1.Contains(new UFile("C:/a/b/x")));
            Assert.False(dir1.Contains(new UFile("C:/a/b/cx")));
        }

        [Fact]
        public void TestGetDirectoryName()
        {
            Assert.Equal("c", new UDirectory("C:/a/b/c").GetDirectoryName());
            Assert.Equal("b", new UDirectory("C:/a/b/").GetDirectoryName());
            Assert.Equal("", new UDirectory("C:/").GetDirectoryName());
            Assert.Equal("a", new UDirectory("/a").GetDirectoryName());
            Assert.Equal("", new UDirectory("/").GetDirectoryName());
            Assert.Equal("a", new UDirectory("//a//").GetDirectoryName());
        }
    }
}
