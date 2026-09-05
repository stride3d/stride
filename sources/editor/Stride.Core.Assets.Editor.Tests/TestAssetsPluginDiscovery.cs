// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Assets.Editor.Services;
using Xunit;

namespace Stride.Core.Assets.Editor.Tests
{
    public class TestAssetsPluginDiscovery
    {
        // Found by the scan
        public sealed class DiscoveredPlugin : AssetsPlugin
        {
        }

        // Skipped: no parameterless constructor
        public sealed class PluginWithArguments : AssetsPlugin
        {
            public PluginWithArguments(int unused) { }
        }

        // Skipped: abstract
        public abstract class PluginBase : AssetsPlugin
        {
        }

        [Fact]
        public void RegisterPluginsFindsConcretePluginsOnce()
        {
            var assembly = typeof(TestAssetsPluginDiscovery).Assembly;

            var first = AssetsPlugin.RegisterPlugins(assembly);
            Assert.Contains(first, x => x is DiscoveredPlugin);
            Assert.DoesNotContain(first, x => x is PluginWithArguments);
            Assert.Single(AssetsPlugin.RegisteredPlugins, x => x is DiscoveredPlugin);

            // Already registered types are left alone
            var second = AssetsPlugin.RegisterPlugins(assembly);
            Assert.Empty(second);
            Assert.Single(AssetsPlugin.RegisteredPlugins, x => x is DiscoveredPlugin);
        }

        [Fact]
        public void RegisterMethodsDefaultToAttributeScansOfThePluginAssembly()
        {
            var plugin = new DiscoveredPlugin();
            var types = new System.Collections.Generic.Dictionary<Type, Type>();
            plugin.RegisterAssetViewModelTypes(types);
            Assert.Empty(types);

            var primitives = new System.Collections.Generic.List<Type>();
            plugin.RegisterPrimitiveTypes(primitives);
            Assert.Empty(primitives);
        }
    }
}
