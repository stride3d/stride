// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization;
using Xenko.Core.Serialization.Contents;
using Xenko.Rendering;
using Xenko.Rendering.ComputeEffect.GGXPrefiltering;
using Xenko.Rendering.ComputeEffect.LambertianPrefiltering;
using Xenko.Rendering.Skyboxes;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Graphics.Data;
using Xenko.Rendering.Materials;
using Xenko.Shaders;
using Xenko.Shaders.Compiler;

namespace Xenko.Assets.Skyboxes
{
    public class SkyboxGeneratorContext : ShaderGeneratorContext
    {
        public SkyboxGeneratorContext(SkyboxAsset skybox, IDatabaseFileProviderService fileProviderService)
        {
            Skybox = skybox ?? throw new ArgumentNullException(nameof(skybox));
            Services = new ServiceRegistry();
            Services.AddService(fileProviderService);
            Content = new ContentManager(Services);
            Services.AddService<IContentManager>(Content);
            Services.AddService(Content);

            GraphicsDevice = GraphicsDevice.New();
            GraphicsDeviceService = new GraphicsDeviceServiceLocal(Services, GraphicsDevice);
            Services.AddService(GraphicsDeviceService);

            var graphicsContext = new GraphicsContext(GraphicsDevice);
            Services.AddService(graphicsContext);

            EffectSystem = new EffectSystem(Services);
            EffectSystem.Compiler = EffectCompilerFactory.CreateEffectCompiler(Content.FileProvider, EffectSystem);

            Services.AddService(EffectSystem);
            EffectSystem.Initialize();
            ((IContentable)EffectSystem).LoadContent();
            ((EffectCompilerCache)EffectSystem.Compiler).CompileEffectAsynchronously = false;

            RenderContext = RenderContext.GetShared(Services);
            RenderDrawContext = new RenderDrawContext(Services, RenderContext, graphicsContext);
        }

        public IServiceRegistry Services { get; private set; }

        public EffectSystem EffectSystem { get; private set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public IGraphicsDeviceService GraphicsDeviceService { get; private set; }

        public RenderContext RenderContext { get; private set; }

        public RenderDrawContext RenderDrawContext { get; private set; }

        public SkyboxAsset Skybox { get; }

        protected override void Destroy()
        {
            EffectSystem.Dispose();
            GraphicsDevice.Dispose();

            base.Destroy();
        }
    }

    public class SkyboxResult : LoggerResult
    {
        public Skybox Skybox { get; set; }
    }

    public class SkyboxGenerator
    {
        public static SkyboxResult Compile(SkyboxAsset asset, SkyboxGeneratorContext context)
        {
            if (asset == null) throw new ArgumentNullException("asset");
            if (context == null) throw new ArgumentNullException("context");
            var result = new SkyboxResult { Skybox = new Skybox() };

            var parameters = context.Parameters;
            var skybox = result.Skybox;
            skybox.Parameters = parameters;
            
            var cubemap = asset.CubeMap;
            if (cubemap == null)
            {
                return result;
            }

            // load the skybox texture from the asset.
            var reference = AttachedReferenceManager.GetAttachedReference(cubemap);
            var skyboxTexture = context.Content.Load<Texture>(BuildTextureForSkyboxGenerationLocation(reference.Url), ContentManagerLoaderSettings.StreamingDisabled);
            if (skyboxTexture.ViewDimension == TextureDimension.Texture2D)
            {
                var cubemapSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(skyboxTexture.Width / 4) / Math.Log(2))); // maximum resolution is around horizontal middle line which composes 4 images.
                skyboxTexture = CubemapFromTextureRenderer.GenerateCubemap(context.Services, context.RenderDrawContext, skyboxTexture, cubemapSize);
            }
            else if (skyboxTexture.ViewDimension != TextureDimension.TextureCube)
            {
                result.Error($"SkyboxGenerator: The texture type ({skyboxTexture.ViewDimension}) used as skybox is not supported. Should be a Cubemap or a 2D texture.");
                return result;
            }

            // If we are using the skybox asset for lighting, we can compute it
            // Specular lighting only?
            if (!asset.IsSpecularOnly)
            {
                // -------------------------------------------------------------------
                // Calculate Diffuse prefiltering
                // -------------------------------------------------------------------
                var lamberFiltering = new LambertianPrefilteringSHNoCompute(context.RenderContext)
                {
                    HarmonicOrder = (int)asset.DiffuseSHOrder,
                    RadianceMap = skyboxTexture
                };
                lamberFiltering.Draw(context.RenderDrawContext);

                var coefficients = lamberFiltering.PrefilteredLambertianSH.Coefficients;
                for (int i = 0; i < coefficients.Length; i++)
                {
                    coefficients[i] = coefficients[i]*SphericalHarmonics.BaseCoefficients[i];
                }

                skybox.DiffuseLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("SphericalHarmonicsEnvironmentColor", lamberFiltering.HarmonicOrder));
                skybox.DiffuseLightingParameters.Set(SphericalHarmonicsEnvironmentColorKeys.SphericalColors, coefficients);
            }

            // -------------------------------------------------------------------
            // Calculate Specular prefiltering
            // -------------------------------------------------------------------
            var specularRadiancePrefilterGGX = new RadiancePrefilteringGGXNoCompute(context.RenderContext);

            var textureSize = asset.SpecularCubeMapSize <= 0 ? 64 : asset.SpecularCubeMapSize;
            textureSize = (int)Math.Pow(2, Math.Round(Math.Log(textureSize, 2)));
            if (textureSize < 64) textureSize = 64;

            // TODO: Add support for HDR 32bits 
            var filteringTextureFormat = skyboxTexture.Format.IsHDR() ? skyboxTexture.Format : PixelFormat.R8G8B8A8_UNorm;

            //var outputTexture = Texture.New2D(graphicsDevice, 256, 256, skyboxTexture.Format, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, 6);
            using (var outputTexture = Texture.New2D(context.GraphicsDevice, textureSize, textureSize, true, filteringTextureFormat, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 6))
            {
                specularRadiancePrefilterGGX.RadianceMap = skyboxTexture;
                specularRadiancePrefilterGGX.PrefilteredRadiance = outputTexture;
                specularRadiancePrefilterGGX.Draw(context.RenderDrawContext);

                var cubeTexture = Texture.NewCube(context.GraphicsDevice, textureSize, true, filteringTextureFormat);
                context.RenderDrawContext.CommandList.Copy(outputTexture, cubeTexture);

                cubeTexture.SetSerializationData(cubeTexture.GetDataAsImage(context.RenderDrawContext.CommandList));

                skybox.SpecularLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("RoughnessCubeMapEnvironmentColor"));
                skybox.SpecularLightingParameters.Set(SkyboxKeys.CubeMap, cubeTexture);
            }
            // TODO: cubeTexture is not deallocated

            return result;
        }

        public static string BuildTextureForSkyboxGenerationLocation(string textureLocation)
        {
            return textureLocation + "__ForSkyboxCompilation__";
        }
    }
}
