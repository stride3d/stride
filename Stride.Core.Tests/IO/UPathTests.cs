using System;
using System.Collections.Generic;
using NUnit.Framework;
using Stride.Core.IO;

namespace Stride.Core.Tests.IO
{
    [TestFixture]
    public class UPathTests
    {
        [Test]
        public void Equals_UnicodeEquivalence_ReturnsTrue()
        {
            // Greek sigma and final sigma are Unicode equivalent under case-insensitive comparison
            var a = new UFile("Assets/\u03C3.png"); // SIGMA
            var b = new UFile("Assets/\u03C2.png"); // FINAL SIGMA
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_SupplementaryPlane_ReturnsConsistentHash()
        {
            // Deseret characters in supplementary plane
            var a = new UFile("Assets/\U00010400.png"); // DESERET CAPITAL LETTER PE
            var b = new UFile("Assets/\U00010428.png"); // DESERET CAPITAL LETTER PE (case variant)
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void Dictionary_Lookup_WorksWithUnicodeEquivalentKeys()
        {
            var dict = new Dictionary<UFile, int>();
            var key1 = new UFile("Assets/\u03C3.png"); // SIGMA
            var key2 = new UFile("Assets/\u03C2.png"); // FINAL SIGMA

            dict[key1] = 42;
            Assert.IsTrue(dict.ContainsKey(key2));
            Assert.AreEqual(42, dict[key2]);
        }

        [Test]
        public void HashSet_ContainsOnlyOneEntryForUnicodeEquivalentPaths()
        {
            var set = new HashSet<UFile>
            {
                new UFile("Assets/\u03C3.png"), // SIGMA
                new UFile("Assets/\u03C2.png")  // FINAL SIGMA
            };
            Assert.AreEqual(1, set.Count);
        }

        [Test]
        public void UDirectory_EqualityAndHashing_ConsistentWithUFile()
        {
            var dir1 = new UDirectory("Assets/\u03C3");
            var dir2 = new UDirectory("Assets/\u03C2");
            Assert.IsTrue(dir1.Equals(dir2));
            Assert.AreEqual(dir1.GetHashCode(), dir2.GetHashCode());
        }
    }
}
