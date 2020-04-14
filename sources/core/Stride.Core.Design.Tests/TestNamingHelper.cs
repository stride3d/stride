// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Design.Tests
{
    public class TestNamingHelper
    {
        [Fact]
        public void TestIdentifier()
        {
            Assert.True(NamingHelper.IsIdentifier("_"));
            Assert.True(NamingHelper.IsIdentifier("a"));
            Assert.True(NamingHelper.IsIdentifier("aThisIsOk"));
            Assert.True(NamingHelper.IsIdentifier("aThis_IsOk"));
            Assert.True(NamingHelper.IsIdentifier("ThisIsOk"));
            Assert.True(NamingHelper.IsIdentifier("T"));
            Assert.True(NamingHelper.IsIdentifier("_a"));
            Assert.True(NamingHelper.IsIdentifier("_aThisIsOk987"));

            Assert.False(NamingHelper.IsIdentifier(""));
            Assert.False(NamingHelper.IsIdentifier("9"));
            Assert.False(NamingHelper.IsIdentifier("a "));
            Assert.False(NamingHelper.IsIdentifier("a x"));
            Assert.False(NamingHelper.IsIdentifier("9aaaaa"));
            Assert.False(NamingHelper.IsIdentifier("9aa.aaa"));
            Assert.False(NamingHelper.IsIdentifier("9aa.aaa"));
        }

        [Fact]
        public void TestNamespace()
        {
            Assert.True(NamingHelper.IsValidNamespace("a"));
            Assert.True(NamingHelper.IsValidNamespace("aThisIsOk"));
            Assert.True(NamingHelper.IsValidNamespace("aThis._IsOk"));
            Assert.True(NamingHelper.IsValidNamespace("a.b.c"));

            Assert.False(NamingHelper.IsValidNamespace(""));
            Assert.False(NamingHelper.IsValidNamespace("a   . w"));
            Assert.False(NamingHelper.IsValidNamespace("a e zaThis._IsOk"));
            Assert.False(NamingHelper.IsValidNamespace("9.b.c"));
            Assert.False(NamingHelper.IsValidNamespace("a.b."));
            Assert.False(NamingHelper.IsValidNamespace(".a."));
        }
    }
}
