// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

using Xunit;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Tests that a material built at run time binds real textures.
    /// </summary>
    /// <remarks>
    /// A material feature attaches its texture references with
    /// <c>AttachedReferenceManager.CreateProxyObject</c>, which makes an empty object and marks it as a
    /// proxy. Only a content load turns such a proxy into a loaded object, and <see cref="Material.New"/>
    /// runs the generator outside that path, so it must resolve the references itself.
    /// <para>
    /// This project holds no material asset that references the lookup table, so the table reaches the
    /// build only because <c>Stride.Engine</c> declares it as a root asset. That makes this the place
    /// where both halves have to hold: the table is packaged, and the material resolves it.
    /// </para>
    /// </remarks>
    public class TestMaterialProxyResolution : GraphicTestGameBase
    {
        [Fact]
        public static void EnvironmentLookupTableIsARealTexture()
        {
            var game = new TestMaterialProxyResolution();
            Texture lookupTable = null;

            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;
                await game.Script.NextFrame();

                var material = Material.New(game.GraphicsDevice, DefaultSpecularModelDescriptor(), game.Content);

                lookupTable = material.Passes[0].Parameters
                    .Get(MaterialSpecularMicrofacetEnvironmentGGXLUTKeys.EnvironmentLightingDFG_LUT);

                game.Exit();
            });

            RunGameTest(game);

            Assert.NotNull(lookupTable);

            var reference = AttachedReferenceManager.GetAttachedReference(lookupTable);

            Assert.False(reference is { IsProxy: true },
                         $"The lookup table is still an unresolved proxy: {reference?.Url}");
            Assert.True(lookupTable.Width > 0 && lookupTable.Height > 0,
                        $"The lookup table has no pixels: {lookupTable.Width}x{lookupTable.Height}");
        }

        /// <summary>
        /// Without a content manager there is nothing to load through, so the reference stays a proxy.
        /// The material says so rather than leaving an empty texture to find later.
        /// </summary>
        [Fact]
        public static void WithoutAContentManagerItSaysSo()
        {
            var game = new TestMaterialProxyResolution();
            var warnings = new List<string>();

            void Collect(ILogMessage message)
            {
                if (message.Type >= LogMessageType.Warning)
                {
                    lock (warnings) warnings.Add(message.Text);
                }
            }

            GlobalLogger.GlobalMessageLogged += Collect;
            try
            {
                game.Script.AddTask(async () =>
                {
                    game.ScreenShotAutomationEnabled = false;
                    await game.Script.NextFrame();

                    Material.New(game.GraphicsDevice, DefaultSpecularModelDescriptor());

                    game.Exit();
                });

                RunGameTest(game);
            }
            finally
            {
                GlobalLogger.GlobalMessageLogged -= Collect;
            }

            Assert.Contains(warnings, text => text.Contains("StrideEnvironmentLightingDFGLUT"));
        }

        /// <summary>
        /// A metal material with the default specular model, whose default environment function is
        /// <c>MaterialSpecularMicrofacetEnvironmentGGXLUT</c>.
        /// </summary>
        private static MaterialDescriptor DefaultSpecularModelDescriptor() => new()
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(1.0f)),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.9f)),
                SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
            },
        };
    }
}
