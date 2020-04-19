// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Particles.Materials
{
    /// <summary>
    /// <see cref="ParticleMaterialComputeColor"/> uses a <see cref="IComputeColor"/> tree to calculate the pixel's emissive value
    /// </summary>
    [DataContract("ParticleMaterialComputeColor")]
    [Display("Emissive Map")]
    public class ParticleMaterialComputeColor : ParticleMaterialSimple
    {
        // TODO Part of the graphics improvement XK-3052
        private int shadersUpdateCounter;

        // TODO Part of the graphics improvement XK-3052
        private ShaderSource shaderSource;

        [DataMemberIgnore]
        public override string EffectName { get; protected set; } = "ParticleEffect";

        /// <summary>
        /// <see cref="IComputeColor"/> allows several channels to be blended together, including textures, vertex streams and fixed values.
        /// Emissive Map should be allowed to be None because some particles might not need to render, but be used as parents for other particle systems
        /// </summary>
        /// <userdoc>
        /// Emissive component ignores light and defines a fixed color this particle should use (emit) when rendered.
        /// </userdoc>
        [DataMember(100)]
        [Display("Emissive Map")]
        public IComputeColor ComputeColor { get; set; } = new ComputeTextureColor();

        /// <summary>
        /// <see cref="Materials.UVBuilder"/> defines how the base coordinates of the particle shape should be modified for texture scrolling, animation, etc.
        /// </summary>
        /// <userdoc>
        /// If left blank, the texture coordinates will be the original ones from the shape builder, usually (0, 0, 1, 1). Or you can define a custom texture coordinate builder which modifies the original coordinates for the sprite.
        /// </userdoc>
        [DataMember(200)]
        [Display("UV coords")]
        public UVBuilder UVBuilder { get; set; }

        /// <summary>
        /// Forces the creation of texture coordinates as vertex attribute
        /// </summary>
        /// <userdoc>
        /// Forces the creation of texture coordinates as vertex attribute
        /// </userdoc>
        [DataMember(300)]
        [Display("Force texcoords")]
        public bool ForceTexCoords { get; set; } = false;

        /// <inheritdoc />
        protected override void InitializeCore(RenderContext context)
        {
            base.InitializeCore(context);

            UpdateShaders(context.GraphicsDevice);
        }

        /// <inheritdoc />
        public override void Setup(RenderContext context)
        {
            base.Setup(context);

            UpdateShaders(context.GraphicsDevice);
        }

        /// <summary>
        /// Polls the shader generator if the shader code has changed and has to be reloaded
        /// </summary>
        /// <param name="graphicsDevice">The current <see cref="GraphicsDevice"/></param>
        private void UpdateShaders(GraphicsDevice graphicsDevice)
        {
            // TODO Part of the graphics improvement XK-3052
            // Don't do this every frame, we have to propagate changes better
            if (--shadersUpdateCounter > 0)
                return;
            shadersUpdateCounter = 10;

            if (ComputeColor != null)
            {
                if (ComputeColor.HasChanged)
                {
                    var shaderGeneratorContext = new ShaderGeneratorContext(graphicsDevice)
                    {
                        Parameters = Parameters,
                        ColorSpace = graphicsDevice.ColorSpace
                    };

                    shaderSource = ComputeColor.GenerateShaderSource(shaderGeneratorContext, new MaterialComputeColorKeys(ParticleBaseKeys.EmissiveMap, ParticleBaseKeys.EmissiveValue, Color.White));

                    if (Parameters.Get(ParticleBaseKeys.BaseColor)?.Equals(shaderSource) ?? false)
                    {
                        shaderSource = Parameters.Get(ParticleBaseKeys.BaseColor);
                    }
                    else
                    {
                        Parameters.Set(ParticleBaseKeys.BaseColor, shaderSource);
                    }

                    HasVertexLayoutChanged = true;
                }
            }
            else
            {
                shaderSource = null;
            }
        }

        /// <inheritdoc />
        public override void UpdateVertexBuilder(ParticleVertexBuilder vertexBuilder)
        {
            base.UpdateVertexBuilder(vertexBuilder);

            // TODO Part of the graphics improvement XK-3052
            //  Ideally, the whole code here should be extracting information from the ShaderBytecode instead as it is quite unreliable and hacky to extract semantics with text matching.
            //  The arguments we need are in the GenericArguments, which is again just an array of strings
            //  We could search it element by element, but in the end getting the entire string and searching it instead is the same
            {
                // 95% of all particle effects will require both texture coordinates and vertex color, so we can add it to the layout here
                // Possible optimization can be detecting material changes
                vertexBuilder.AddVertexElement(ParticleVertexElements.Color);
                vertexBuilder.AddVertexElement(ParticleVertexElements.TexCoord[0]);
            } // Part of the graphics improvement XK-3052

        }

        public override void ValidateEffect(RenderContext context, ref EffectValidator effectValidator)
        {
            base.ValidateEffect(context, ref effectValidator);

            effectValidator.ValidateParameter(ParticleBaseKeys.BaseColor, shaderSource);
        }

        /// <inheritdoc />
        public unsafe override void PatchVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY, ref ParticleList sorter)
        {
            // If you want, you can implement the base builder here and not call it. It should result in slight speed up
            base.PatchVertexBuffer(ref bufferState, invViewX, invViewY, ref sorter);

            //  The UV Builder, if present, animates the basic (0, 0, 1, 1) uv coordinates of each billboard
            UVBuilder?.BuildUVCoordinates(ref bufferState, ref sorter, bufferState.DefaultTexCoords);
            bufferState.StartOver();

            // If the particles have color field, the base class should have already passed the information
            if (HasColorField)
                return;

            // If the particles don't have color field but there is no color stream either we don't need to fill anything
            var colAttribute = bufferState.GetAccessor(VertexAttributes.Color);
            if (colAttribute.Size <= 0)
                return;

            // Since the particles don't have their own color field, set the default color to white
            var color = 0xFFFFFFFF;

            // TODO: for loop. Remove IEnumerable from sorter
            foreach (var particle in sorter)
            {
                bufferState.SetAttributePerParticle(colAttribute, (IntPtr)(&color));

                bufferState.NextParticle();
            }

            bufferState.StartOver();
        }

    }
}
