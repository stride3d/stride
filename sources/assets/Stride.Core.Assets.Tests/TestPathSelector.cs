// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Xunit;
using Stride.Core.Assets.Selectors;
using Stride.Core.Storage;

namespace Stride.Core.Assets.Tests
{
    public class TestPathSelector
    {
        public bool TestSingleUrl(PathSelector pathSelector, string asset)
        {
            var assetIndexMap = new ObjectDatabaseContentIndexMap();
            assetIndexMap[asset] = ObjectId.New();

            return pathSelector.Select(null, assetIndexMap).Count() == 1;
        }

        private void TestPatterns(PathSelector pathSelector, string pattern, bool shouldMatch, bool allowPrefix, bool allowSuffix)
        {
            if (shouldMatch)
            {
                Assert.True(TestSingleUrl(pathSelector, pattern));
                Assert.True(TestSingleUrl(pathSelector, "a/" + pattern));
                Assert.True(TestSingleUrl(pathSelector, pattern + "/b"));
                Assert.True(TestSingleUrl(pathSelector, "a/" + pattern + "/b"));
            }
            else
            {
                Assert.False(TestSingleUrl(pathSelector, pattern));
                Assert.False(TestSingleUrl(pathSelector, "a/" + pattern));
                Assert.False(TestSingleUrl(pathSelector, pattern + "/b"));
                Assert.False(TestSingleUrl(pathSelector, "a/" + pattern + "/b"));
            }

            if (allowSuffix)
            {
                Assert.False(TestSingleUrl(pathSelector, pattern + "A"));
                Assert.False(TestSingleUrl(pathSelector, "a/" + pattern + "A"));
                Assert.False(TestSingleUrl(pathSelector, pattern + "A/b"));
                Assert.False(TestSingleUrl(pathSelector, "a/" + pattern + "A/b"));
            }

            if (allowPrefix)
            {
                Assert.False(TestSingleUrl(pathSelector, "A" + pattern));
                Assert.False(TestSingleUrl(pathSelector, "a/A" + pattern));
                Assert.False(TestSingleUrl(pathSelector, "A" + pattern + "/b"));
                Assert.False(TestSingleUrl(pathSelector, "a/A" + pattern + "/b"));
            }
        }

        [Fact]
        public void TestSimple()
        {
            var pathSelector = new PathSelector();
            pathSelector.Paths.Add("simple");

            TestPatterns(pathSelector, "simple", true, true, true);
        }

        [Fact]
        public void TestDouble()
        {
            var pathSelector = new PathSelector();
            pathSelector.Paths.Add("ab/cd");

            TestPatterns(pathSelector, "ab/cd", true, true, true);
        }

        [Fact]
        public void TestStartEnd()
        {
            var pathSelector = new PathSelector();
            pathSelector.Paths.Add("/ab/cd");

            Assert.True(TestSingleUrl(pathSelector, "ab/cd"));
            Assert.True(TestSingleUrl(pathSelector, "ab/cd/de"));
            Assert.False(TestSingleUrl(pathSelector, "test/ab/cd"));

            pathSelector.Paths.Add("ab/cd/");
            Assert.False(TestSingleUrl(pathSelector, "xx/ab/cd"));
            Assert.True(TestSingleUrl(pathSelector, "xx/ab/cd/"));
            Assert.True(TestSingleUrl(pathSelector, "xx/ab/cd/de"));
        }

        [Fact]
        public void TestEscape()
        {
            var pathSelector = new PathSelector();
            pathSelector.Paths.Add(@"\?\?");

            Assert.True(TestSingleUrl(pathSelector, "??"));
            Assert.False(TestSingleUrl(pathSelector, "ab"));
        }

        [Fact]
        public void TestWildcard()
        {
            var pathSelector = new PathSelector();
            pathSelector.Paths.Add("*.test");

            TestPatterns(pathSelector, "a.test", true, false, true);
            TestPatterns(pathSelector, "a.test2", false, false, true);

            pathSelector.Paths[0] = "???.test";
            TestPatterns(pathSelector, "abc.test", true, false, true);
            TestPatterns(pathSelector, "ab.test", false, false, true);

            pathSelector.Paths[0] = "test.*";
            TestPatterns(pathSelector, "test.a", true, true, false);
            TestPatterns(pathSelector, "test2.a", false, true, false);
        }
    }
}
