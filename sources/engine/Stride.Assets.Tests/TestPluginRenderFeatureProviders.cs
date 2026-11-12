// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Stride.Assets.Rendering;
using Stride.Core.Reflection;
using Stride.Rendering;
using Stride.Rendering.UI;
using Stride.SpriteStudio.Assets;
using Stride.SpriteStudio.Runtime;
using Stride.UI.Assets;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The render features of the engine's plugins come from packages that always load, the runtimes and their Assets
    /// packages: the scan finds each provider with no Editor package loaded, so the scene editor, previews, thumbnails and
    /// new compositors draw the plugin's content even when its Editor package is left out.
    /// </summary>
    public class TestPluginRenderFeatureProviders
    {
        [Theory]
        [InlineData(typeof(UIRenderFeatureProvider), typeof(UIRenderFeature))]
        [InlineData(typeof(SpriteStudioRenderFeatureProvider), typeof(SpriteStudioRenderFeature))]
        public void TheAssetsPackageProvidesTheRenderFeature(Type providerType, Type renderFeatureType)
        {
            using (AssetAssemblyScope.Register(providerType.Assembly))
            {
                var provider = Assert.Single(RenderFeatureProviders.Enumerate(), x => x.GetType() == providerType);
                var opaqueStage = new RenderStage("Opaque", "Main");
                var transparentStage = new RenderStage("Transparent", "Main");

                var renderFeature = Assert.Single(provider.CreateRenderFeatures(opaqueStage, transparentStage));

                Assert.IsType(renderFeatureType, renderFeature);
            }

            // This project references no Editor package: nothing above came from one
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name is { } name && name.StartsWith("Stride.", StringComparison.Ordinal) && name.EndsWith(".Editor", StringComparison.Ordinal));
        }

        /// <summary>
        /// Registers an assembly as an asset assembly, as loading its package does, and unregisters it at the end unless it
        /// was registered already.
        /// </summary>
        internal sealed class AssetAssemblyScope : IDisposable
        {
            private readonly Assembly assembly;
            private readonly bool registered;

            private AssetAssemblyScope(Assembly assembly, bool registered)
            {
                this.assembly = assembly;
                this.registered = registered;
            }

            public static AssetAssemblyScope Register(Assembly assembly)
            {
                // Its module initializer registers its scan index
                RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
                if (AssemblyRegistry.Find(AssemblyCommonCategories.Assets).Contains(assembly))
                    return new AssetAssemblyScope(assembly, registered: false);

                AssemblyRegistry.Register(assembly, AssemblyCommonCategories.Assets);
                return new AssetAssemblyScope(assembly, registered: true);
            }

            public void Dispose()
            {
                if (registered)
                    AssemblyRegistry.Unregister(assembly);
            }
        }
    }
}
