using System;
using NUnit.Framework;
using Stride.Core.Design;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Design.Tests.Settings
{
    [TestFixture]
    public class SettingsContainerTests
    {
        [Test]
        public void GetSettingsKey_UnicodeEquivalentPaths_ReturnsSameKey()
        {
            var container = new SettingsContainer();
            var key1 = new SettingsKey<int>("Settings/\u03C3", container, 42); // SIGMA
            var key2 = container.GetSettingsKey("Settings/\u03C2"); // FINAL SIGMA

            Assert.IsNotNull(key2);
            Assert.AreEqual(key1, key2);
            Assert.AreSame(key1, key2);
        }

        [Test]
        public void SettingsKey_EqualityAndHashing_ConsistentWithUPath()
        {
            var container = new SettingsContainer();
            var key1 = new SettingsKey<int>("Settings/\U00010400", container, 42); // DESERET PE
            var key2 = new SettingsKey<int>("Settings/\U00010428", container, 43); // DESERET PE (case variant)

            Assert.IsTrue(key1.Equals(key2));
            Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
        }
    }
}
