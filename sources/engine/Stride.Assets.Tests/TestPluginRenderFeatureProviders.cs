// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Assets.Rendering;
using Stride.Particles.Assets;
using Stride.Particles.Rendering;
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
        [InlineData(typeof(ParticleRenderFeatureProvider), typeof(ParticleEmitterRenderFeature))]
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
            Assert.False(AssetAssemblyScope.AnyEditorPackageLoaded());
        }
    }
}
