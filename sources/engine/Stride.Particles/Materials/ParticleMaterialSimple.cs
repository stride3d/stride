// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;
using Stride.Rendering;

namespace Stride.Particles.Materials
{
    public enum ParticleMaterialCulling : byte
    {
        CullNone = 0,
        CullBack = 1,
        CullFront = 2
    }

    /// <summary>
    /// Simple base for most of the particle material classes which uses additive-alpha blending, face culling and setups the color vertex stream
    /// </summary>
    [DataContract("ParticleMaterialSimple")]
    public abstract class ParticleMaterialSimple : ParticleMaterial
    {
        /// <summary>
        /// Shows if the particles should be rendered as alhpa-blended, additive or something in-between (lerp between the two methods)
        /// </summary>
        /// <userdoc>
        /// Defines if the particles should be rendered as alpha-blended (0), additive (1) or something in-between (any value between 0 and 1)
        /// </userdoc>
        [DataMember(20)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Alpha-Add")]
        [DefaultValue(0)]
        public float AlphaAdditive { get; set; }

        /// <summary>
        /// Adjusts the depth of the particle in regard to opaque objects
        /// </summary>
        /// <userdoc>
        /// Adjusts the depth of the particle in regard to opaque objects
        /// </userdoc>
        [DataMember(30)]
        [Display("Z Offset")]
        [DefaultValue(0)]
        public float ZOffset { get; set; } = 0f;

        /// <summary>
        /// If positive, soft particle edges will be calculated with maximum distance of the value set.
        /// </summary>
        /// <userdoc>
        /// If positive, soft particle edges will be calculated with maximum distance of the value set.
        /// </userdoc>
        [DataMember(35)]
        [Display("Soft Edge")]
        [DefaultValue(0)]
        public float SoftEdgeDistance { get; set; } = 0f;

        /// <summary>
        /// Allows the particle shape to be back- or front-face culled.
        /// </summary>
        /// <userdoc>
        /// The default option is no culling, other possible options are back-face culling and front-face culling. Culling both faces at the same time is not an option.
        /// </userdoc>
        [DataMember(40)]
        [Display("Culling")]
        [DefaultValue(ParticleMaterialCulling.CullNone)]
        public ParticleMaterialCulling FaceCulling { get; set; }

        /// <summary>
        /// Indicates if this material requires a color field in the vertex stream
        /// </summary>
        protected bool HasColorField { get; private set; }

        /// <inheritdoc />
        public override void PrepareVertexLayout(ParticlePoolFieldsList fieldsList)
        {
            base.PrepareVertexLayout(fieldsList);

            // Probe if the particles have a color field and if we need to support it
            var colorField = fieldsList.GetField(ParticleFields.Color);
            if (colorField.IsValid() != HasColorField)
            {
                HasVertexLayoutChanged = true;
                HasColorField = colorField.IsValid();
            }
        }

        /// <inheritdoc/>
        public override void ForceUpdate()
        {
            base.ForceUpdate();
            particleMaterialSimpleHasChanged = true;
        }

        private bool particleMaterialSimpleHasChanged = true;

        /// <inheritdoc />
        public override void Setup(RenderContext context)
        {
            base.Setup(context);

            if (!particleMaterialSimpleHasChanged)
                return;
            particleMaterialSimpleHasChanged = false;

            // This is correct. We invert the value here to reduce calculations on the shader side later
            Parameters.Set(ParticleBaseKeys.AlphaAdditive, 1f - AlphaAdditive);

            Parameters.Set(ParticleBaseKeys.ZOffset, ZOffset);

            // This is correct. We invert the value here to reduce calculations on the shader side later
            Parameters.Set(ParticleBaseKeys.SoftEdgeInverseDistance, (SoftEdgeDistance > 0) ? (1f / SoftEdgeDistance) : 0f);
        }

        public override void ValidateEffect(RenderContext context, ref EffectValidator effectValidator)
        {
            base.ValidateEffect(context, ref effectValidator);

            effectValidator.ValidateParameter(ParticleBaseKeys.UsesSoftEdge, (SoftEdgeDistance > 0) ? 1u : 0u);
        }

        public override void SetupPipeline(RenderContext renderContext, PipelineStateDescription pipelineState)
        {
            base.SetupPipeline(renderContext, pipelineState);

            if (FaceCulling == ParticleMaterialCulling.CullNone) pipelineState.RasterizerState = RasterizerStates.CullNone;
            else if (FaceCulling == ParticleMaterialCulling.CullBack) pipelineState.RasterizerState = RasterizerStates.CullBack;
            else if (FaceCulling == ParticleMaterialCulling.CullFront) pipelineState.RasterizerState = RasterizerStates.CullFront;

            pipelineState.BlendState = BlendStates.AlphaBlend;

            pipelineState.DepthStencilState = DepthStencilStates.DepthRead;
        }

        /// <inheritdoc />
        public override unsafe void PatchVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY, ref ParticleList sorter)
        {
            // If you want, you can integrate the base builder here and not call it. It should result in slight speed up
            base.PatchVertexBuffer(ref bufferState, invViewX, invViewY, ref sorter);

            var colorField = sorter.GetField(ParticleFields.Color);
            if (!colorField.IsValid())
                return;

            var colAttribute = bufferState.GetAccessor(VertexAttributes.Color);
            if (colAttribute.Size <= 0)
                return;

            foreach (var particle in sorter)
            {
                // Set the vertex color attribute to the particle's color field
                var color = (uint)(*(Color4*)particle[colorField]).ToRgba();
                bufferState.SetAttributePerSegment(colAttribute, (IntPtr)(&color));

                bufferState.NextSegment();
            }

            bufferState.StartOver();
        }
    }
}
