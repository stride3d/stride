// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;

namespace Xenko.Core.Design.Tests
{
    [TestFixture]
    public class TestNamingHelper
    {
        [Test]
        public void TestIdentifier()
        {
            Assert.IsTrue(NamingHelper.IsIdentifier("_"));
            Assert.IsTrue(NamingHelper.IsIdentifier("a"));
            Assert.IsTrue(NamingHelper.IsIdentifier("aThisIsOk"));
            Assert.IsTrue(NamingHelper.IsIdentifier("aThis_IsOk"));
            Assert.IsTrue(NamingHelper.IsIdentifier("ThisIsOk"));
            Assert.IsTrue(NamingHelper.IsIdentifier("T"));
            Assert.IsTrue(NamingHelper.IsIdentifier("_a"));
            Assert.IsTrue(NamingHelper.IsIdentifier("_aThisIsOk987"));

            Assert.IsFalse(NamingHelper.IsIdentifier(""));
            Assert.IsFalse(NamingHelper.IsIdentifier("9"));
            Assert.IsFalse(NamingHelper.IsIdentifier("a "));
            Assert.IsFalse(NamingHelper.IsIdentifier("a x"));
            Assert.IsFalse(NamingHelper.IsIdentifier("9aaaaa"));
            Assert.IsFalse(NamingHelper.IsIdentifier("9aa.aaa"));
            Assert.IsFalse(NamingHelper.IsIdentifier("9aa.aaa"));
        }

        [Test]
        public void TestNamespace()
        {
            Assert.IsTrue(NamingHelper.IsValidNamespace("a"));
            Assert.IsTrue(NamingHelper.IsValidNamespace("aThisIsOk"));
            Assert.IsTrue(NamingHelper.IsValidNamespace("aThis._IsOk"));
            Assert.IsTrue(NamingHelper.IsValidNamespace("a.b.c"));

            Assert.IsFalse(NamingHelper.IsValidNamespace(""));
            Assert.IsFalse(NamingHelper.IsValidNamespace("a   . w"));
            Assert.IsFalse(NamingHelper.IsValidNamespace("a e zaThis._IsOk"));
            Assert.IsFalse(NamingHelper.IsValidNamespace("9.b.c"));
            Assert.IsFalse(NamingHelper.IsValidNamespace("a.b."));
            Assert.IsFalse(NamingHelper.IsValidNamespace(".a."));
        }
    }
}
