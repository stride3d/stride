// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using Stride.Shaders.Compiler;

namespace Stride.Rendering
{
    /// <summary>
    /// Builds the effect a <see cref="MeshRenderFeature"/> draws with while a mesh's real effect is still compiling
    /// or failed to compile. It keeps the mesh's diffuse texture or color with Lambert shading and its skinning,
    /// and drops every other material feature, so all meshes share a few permutations that are usually already
    /// compiled and never depend on user shaders.
    /// </summary>
    public class MeshFallbackEffects
    {
        private const int MaxSkinningBones = 56;

        private readonly EffectSystem effectSystem;
        private readonly Material colorMaterial;
        private readonly Material textureMaterial;

        public MeshFallbackEffects(GraphicsDevice graphicsDevice, EffectSystem effectSystem)
        {
            this.effectSystem = effectSystem ?? throw new ArgumentNullException(nameof(effectSystem));

            colorMaterial = Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });

            textureMaterial = Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor { FallbackValue = null }), // Do not use fallback value, we want a DiffuseMap
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
        }

        /// <summary>
        /// Creates the compiler parameters of the fallback effect for <paramref name="renderMesh"/>.
        /// </summary>
        public CompilerParameters CreateCompilerParameters(RenderMesh renderMesh)
        {
            var hasDiffuseMap = renderMesh.MaterialPass.Parameters.ContainsKey(MaterialKeys.DiffuseMap);
            var fallbackMaterial = hasDiffuseMap ? textureMaterial : colorMaterial;

            // High priority
            var compilerParameters = new CompilerParameters();
            compilerParameters.EffectParameters.TaskPriority = -1;

            // Support skinning
            if (renderMesh.Mesh.Skinning != null && renderMesh.Mesh.Skinning.Bones.Length <= MaxSkinningBones)
            {
                compilerParameters.Set(MaterialKeys.HasSkinningPosition, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningPosition));
                compilerParameters.Set(MaterialKeys.HasSkinningNormal, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningNormal));
                compilerParameters.Set(MaterialKeys.HasSkinningTangent, renderMesh.Mesh.Parameters.Get(MaterialKeys.HasSkinningTangent));

                compilerParameters.Set(MaterialKeys.SkinningMaxBones, MaxSkinningBones);
            }

            // Set material permutations
            compilerParameters.Set(MaterialKeys.PixelStageSurfaceShaders, fallbackMaterial.Passes[0].Parameters.Get(MaterialKeys.PixelStageSurfaceShaders));
            compilerParameters.Set(MaterialKeys.PixelStageStreamInitializer, fallbackMaterial.Passes[0].Parameters.Get(MaterialKeys.PixelStageStreamInitializer));

            // Set lighting permutations (use custom white light, since this effect will not be processed by the lighting render feature)
            compilerParameters.Set(LightingKeys.EnvironmentLights, new ShaderSourceCollection { new ShaderClassSource("LightConstantWhite") });

            return compilerParameters;
        }

        /// <summary>
        /// Loads the fallback effect for <paramref name="renderEffect"/>. Matches <see cref="RootEffectRenderFeature.ComputeFallbackEffectDelegate"/>.
        /// </summary>
        /// <returns>The fallback effect, or <c>null</c> when it could not be loaded, in which case the render effect is flagged as errored.</returns>
        public Effect ComputeFallbackEffect(RenderObject renderObject, RenderEffect renderEffect, RenderEffectState renderEffectState)
        {
            return ComputeFallbackEffect(renderObject, renderEffect, renderEffectState, null);
        }

        /// <summary>
        /// Same as <see cref="ComputeFallbackEffect(RenderObject, RenderEffect, RenderEffectState)"/>, with a hook to adjust
        /// the compiler parameters before the effect is loaded.
        /// </summary>
        public Effect ComputeFallbackEffect(RenderObject renderObject, RenderEffect renderEffect, RenderEffectState renderEffectState, Action<RenderEffect, RenderEffectState, CompilerParameters> customizeParameters)
        {
            try
            {
                var renderMesh = (RenderMesh)renderObject;

                var compilerParameters = CreateCompilerParameters(renderMesh);
                customizeParameters?.Invoke(renderEffect, renderEffectState, compilerParameters);

                if (renderEffectState == RenderEffectState.Error)
                {
                    // Retry every few seconds
                    renderEffect.RetryTime = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                }

                var effect = effectSystem.LoadEffect(renderEffect.EffectSelector.EffectName, compilerParameters).WaitForResult();

                // Initialize parameters with material ones (need a CopyTo?)
                renderEffect.FallbackParameters = new ParameterCollection(renderMesh.MaterialPass.Parameters);

                return effect;
            }
            catch
            {
                // TODO: Log or rethrow?
                renderEffect.State = RenderEffectState.Error;
                return null;
            }
        }
    }
}
