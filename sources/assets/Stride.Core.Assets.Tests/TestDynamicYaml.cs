// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using Stride.Core.Reflection;
using Stride.Core.Yaml;

namespace Stride.Core.Design.Tests
{
    /// <summary>
    /// Tests for <see cref="DynamicYaml"/> and <see cref="DynamicYamlMapping"/>
    /// </summary>
    public class TestDynamicYaml
    {
        /// <summary>
        /// Basic test for <see cref="DynamicYaml"/> and <see cref="DynamicYamlMapping"/>
        /// </summary>
        [Fact]
        public void TestSimple()
        {
            var yamlMapping = new DynamicYaml(
                @"name: This is a property
key: second key");

            Assert.Equal(2, yamlMapping.RootNode.Children.Count);

            // Test basic accessor
            dynamic obj = yamlMapping.DynamicRootNode;
            Assert.Equal("This is a property", (string)obj.name);
            Assert.Equal("second key", (string)obj.key);
            Assert.Null((string)obj.invalid);

            // Test remove a key
            var dyn = (DynamicYamlMapping)obj;
            dyn.RemoveChild("name");
            Assert.Null((string)obj.name);

            // Test serialization back to a string
            var text = yamlMapping.ToString().TrimEnd();
            Assert.Equal("key: second key", text);
        }

        /// <summary>
        /// Basic test for <see cref="DynamicYaml"/> and <see cref="DynamicYamlMapping"/>
        /// </summary>
        [Fact]
        public void TestWithOverrides()
        {
            var yamlMapping = new DynamicYaml(
                @"name*: This is a property
key!: second key
override1!*: combine override 1
override2*!: combine override 2
nooverrides: no overrides here!
");

            Assert.Equal(5, yamlMapping.RootNode.Children.Count);

            // Test basic accessor
            dynamic obj = yamlMapping.DynamicRootNode;
            Assert.Equal("This is a property", (string)obj.name);
            Assert.Equal("second key", (string)obj.key);
            Assert.Equal("no overrides here!", (string)obj.nooverrides);
            Assert.Null((string)obj.invalid);

            // Check overrides
            var dyn = (DynamicYamlMapping)obj;
            Assert.Equal(OverrideType.New, dyn.GetOverride("name"));
            Assert.Equal(OverrideType.Sealed, dyn.GetOverride("key"));
            Assert.Equal(OverrideType.New | OverrideType.Sealed, dyn.GetOverride("override1"));
            Assert.Equal(OverrideType.New | OverrideType.Sealed, dyn.GetOverride("override2"));
            Assert.Equal(OverrideType.Base, dyn.GetOverride("nooverrides"));

            // Check that removing a child will remove overrides information
            dyn.RemoveChild("override2");
            Assert.Equal(OverrideType.Base, dyn.GetOverride("override2"));

            // Modify overrides, check that we can still access the values
            dyn.SetOverride("name", OverrideType.Sealed);
            dyn.SetOverride("key", OverrideType.New);
            Assert.Equal("This is a property", (string)obj.name);
            Assert.Equal("second key", (string)obj.key);

            // Test serialization back to a string
            var text = yamlMapping.ToString().TrimEnd().Replace("\r\n", "\n");
            Assert.Equal(@"name!: This is a property
key*: second key
override1!*: combine override 1
nooverrides: no overrides here!
".TrimEnd().Replace("\r\n", "\n"), text);
        }
    }
}
