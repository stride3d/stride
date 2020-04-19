// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Particles.Sorters;
using Stride.Particles.VertexLayouts;

namespace Stride.Particles.ShapeBuilders
{
    /// <summary>
    /// The common shape builder provides additive animation for the particle's position and size fields,
    ///  assuming that all derived shape builders will have position and size fields
    /// </summary>
    [DataContract("ShapeBuilderCommon")]
    public abstract class ShapeBuilderCommon : ShapeBuilder
    {
        /// <inheritdoc />
        public override void PreUpdate()
        {
            SamplerPosition?.UpdateChanges();

            SamplerSize?.UpdateChanges();
        }

        public override int BuildVertexBuffer(ref ParticleBufferState bufferState, Vector3 invViewX, Vector3 invViewY,
            ref Vector3 spaceTranslation, ref Quaternion spaceRotation, float spaceScale, ref ParticleList sorter, ref Matrix viewProj)
        {
            return 0;
        }

        /// <summary>
        /// Additive animation for the particle position. If present, particle's own position will be added to the sampled curve value
        /// </summary>
        /// <userdoc>
        /// Additive animation for the particle position. If present, particle's own position will be added to the sampled curve value
        /// </userdoc>
        [DataMember(100)]
        [Display("Additive Position Animation")]
        public ComputeCurveSampler<Vector3> SamplerPosition { get; set; }

        /// <summary>
        /// Additive animation for the particle size. If present, particle's own size will be multiplied with the sampled curve value
        /// </summary>
        /// <userdoc>
        /// Additive animation for the particle size. If present, particle's own size will be multiplied with the sampled curve value
        /// </userdoc>
        [DataMember(200)]
        [Display("Additive Size Animation")]
        public ComputeCurveSampler<float> SamplerSize { get; set; }

        /// <summary>
        /// Gets the combined position for the particle, adding its field value (if any) to its sampled value from the curve
        /// </summary>
        /// <param name="particle"></param>
        /// <param name="positionField"></param>
        /// <param name="lifeField">Normalized life for sampling</param>
        /// <returns>Particle's current 3D position</returns>
        protected unsafe Vector3 GetParticlePosition(Particle particle, ParticleFieldAccessor<Vector3> positionField, ParticleFieldAccessor<float> lifeField)
        {
            if (SamplerPosition == null)
                return particle.Get(positionField);

            var life = 1f - (*((float*)particle[lifeField]));   // The Life field contains remaining life, so for sampling we take (1 - life)

            return particle.Get(positionField) + SamplerPosition.Evaluate(life);
        }

        /// <summary>
        /// Gets the combined size for the particle, adding its field value (if any) to its sampled value from the curve
        /// </summary>
        /// <param name="particle">Target particle</param>
        /// <param name="sizeField">Size field accessor</param>
        /// <param name="lifeField">Normalized life for sampling</param>
        /// <returns>Particle's current uniform size</returns>
        protected unsafe float GetParticleSize(Particle particle, ParticleFieldAccessor<float> sizeField, ParticleFieldAccessor<float> lifeField)
        {
            var particleSize = sizeField.IsValid() ? particle.Get(sizeField) : 1f;

            if (SamplerSize == null)
                return particleSize;

            var life = 1f - (*((float*)particle[lifeField]));   // The Life field contains remaining life, so for sampling we take (1 - life)

            return particleSize * SamplerSize.Evaluate(life);
        }


    }
}
