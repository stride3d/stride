// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using NUnit.Framework;
using Xenko.Core.Reflection;
using Xenko.Core.Yaml;

namespace Xenko.Core.Design.Tests
{
    /// <summary>
    /// Tests for <see cref="DynamicYaml"/> and <see cref="DynamicYamlMapping"/>
    /// </summary>
    [TestFixture]
    public class TestDynamicYaml
    {
        /// <summary>
        /// Basic test for <see cref="DynamicYaml"/> and <see cref="DynamicYamlMapping"/>
        /// </summary>
        [Test]
        public void TestSimple()
        {
            var yamlMapping = new DynamicYaml(
                @"name: This is a property
key: second key");

            Assert.AreEqual(2, yamlMapping.RootNode.Children.Count);

            // Test basic accessor
            dynamic obj = yamlMapping.DynamicRootNode;
            Assert.AreEqual("This is a property", (string)obj.name);
            Assert.AreEqual("second key", (string)obj.key);
            Assert.Null(null, (string)obj.invalid);

            // Test remove a key
            var dyn = (DynamicYamlMapping)obj;
            dyn.RemoveChild("name");
            Assert.Null(null, (string)obj.name);

            // Test serialization back to a string
            var text = yamlMapping.ToString().TrimEnd();
            Assert.AreEqual("key: second key", text);
        }

        /// <summary>
        /// Basic test for <see cref="DynamicYaml"/> and <see cref="DynamicYamlMapping"/>
        /// </summary>
        [Test]
        public void TestWithOverrides()
        {
            var yamlMapping = new DynamicYaml(
                @"name*: This is a property
key!: second key
override1!*: combine override 1
override2*!: combine override 2
nooverrides: no overrides here!
");

            Assert.AreEqual(5, yamlMapping.RootNode.Children.Count);

            // Test basic accessor
            dynamic obj = yamlMapping.DynamicRootNode;
            Assert.AreEqual("This is a property", (string)obj.name);
            Assert.AreEqual("second key", (string)obj.key);
            Assert.AreEqual("no overrides here!", (string)obj.nooverrides);
            Assert.Null(null, (string)obj.invalid);

            // Check overrides
            var dyn = (DynamicYamlMapping)obj;
            Assert.AreEqual(OverrideType.New, dyn.GetOverride("name"));
            Assert.AreEqual(OverrideType.Sealed, dyn.GetOverride("key"));
            Assert.AreEqual(OverrideType.New | OverrideType.Sealed, dyn.GetOverride("override1"));
            Assert.AreEqual(OverrideType.New | OverrideType.Sealed, dyn.GetOverride("override2"));
            Assert.AreEqual(OverrideType.Base, dyn.GetOverride("nooverrides"));

            // Check that removing a child will remove overrides information
            dyn.RemoveChild("override2");
            Assert.AreEqual(OverrideType.Base, dyn.GetOverride("override2"));

            // Modify overrides, check that we can still access the values
            dyn.SetOverride("name", OverrideType.Sealed);
            dyn.SetOverride("key", OverrideType.New);
            Assert.AreEqual("This is a property", (string)obj.name);
            Assert.AreEqual("second key", (string)obj.key);

            // Test serialization back to a string
            var text = yamlMapping.ToString().TrimEnd().Replace("\r\n", "\n");
            Assert.AreEqual(@"name!: This is a property
key*: second key
override1!*: combine override 1
nooverrides: no overrides here!
".TrimEnd().Replace("\r\n", "\n"), text);
        }
    }
}
