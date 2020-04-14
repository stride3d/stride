// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Animations;
using Stride.Particles.Modules;

namespace Stride.Particles.Updaters
{
    /// <summary>
    /// Updater which sets the particle's size to a fixed value sampled based on the particle's normalized life value
    /// </summary>
    [DataContract("UpdaterSizeOverTime")]
    [Display("Size Animation")]
    public class UpdaterSizeOverTime : ParticleUpdater
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public UpdaterSizeOverTime()
        {
            RequiredFields.Add(ParticleFields.Size);

            var curve = new ComputeAnimationCurveFloat();
            SamplerMain.Curve = curve;
        }


        /// <inheritdoc />
        [DataMemberIgnore]
        public override bool IsPostUpdater => true;

        /// <summary>
        /// The main curve sampler. Particles will change their value based on the sampled values
        /// </summary>
        /// <userdoc>
        /// The main curve sampler. Particles will change their value based on the sampled values
        /// </userdoc>
        [DataMember(100)]
        [NotNull]
        [Display("Main")]
        public ComputeCurveSampler<float> SamplerMain { get; set; } = new ComputeCurveSamplerFloat();

        /// <summary>
        /// Optional sampler. If present, particles will pick a random value between the two sampled curves
        /// </summary>
        /// <userdoc>
        /// Optional sampler. If present, particles will pick a random value between the two sampled curves
        /// </userdoc>
        [DataMember(200)]
        [Display("Optional")]
        public ComputeCurveSampler<float> SamplerOptional { get; set; }

        /// <summary>
        /// Seed offset. You can use this offset to bind the randomness to other random values, or to make them completely unrelated
        /// </summary>
        /// <userdoc>
        /// Seed offset. You can use this offset to bind the randomness to other random values, or to make them completely unrelated
        /// </userdoc>
        [DataMember(300)]
        [Display("Random Seed")]
        public uint SeedOffset { get; set; } = 0;

        /// <inheritdoc />
        public override void PreUpdate()
        {
            base.PreUpdate();

            SamplerMain?.UpdateChanges();
            SamplerOptional?.UpdateChanges();
        }

        /// <inheritdoc />
        public override void Update(float dt, ParticlePool pool)
        {
            if (!pool.FieldExists(ParticleFields.Size) || !pool.FieldExists(ParticleFields.Life))
                return;

            if (SamplerOptional == null)
            {
                UpdateSingleSampler(pool);
                return;
            }

            UpdateDoubleSampler(pool);
        }

        /// <summary>
        /// Updates the field by sampling a single value over the particle's lifetime
        /// </summary>
        /// <param name="pool">Target <see cref="ParticlePool"/></param>
        private unsafe void UpdateSingleSampler(ParticlePool pool)
        {
            var sizeField = pool.GetField(ParticleFields.Size);
            var lifeField = pool.GetField(ParticleFields.Life);

            foreach (var particle in pool)
            {
                var life = 1f - (*((float*)particle[lifeField]));   // The Life field contains remaining life, so for sampling we take (1 - life)

                (*((float*)particle[sizeField])) = WorldScale.X * SamplerMain.Evaluate(life);
            }
        }

        /// <summary>
        /// Updates the field by interpolating between two sampled values over the particle's lifetime
        /// </summary>
        /// <param name="pool">Target <see cref="ParticlePool"/></param>
        private unsafe void UpdateDoubleSampler(ParticlePool pool)
        {
            var sizeField = pool.GetField(ParticleFields.Size);
            var lifeField = pool.GetField(ParticleFields.Life);
            var randField = pool.GetField(ParticleFields.RandomSeed);

            foreach (var particle in pool)
            {
                var life = 1f - (*((float*)particle[lifeField]));   // The Life field contains remaining life, so for sampling we take (1 - life)

                var randSeed = particle.Get(randField);
                var lerp = randSeed.GetFloat(RandomOffset.Offset1A + SeedOffset);

                var size1 = SamplerMain.Evaluate(life);
                var size2 = SamplerOptional.Evaluate(life);

                (*((float*)particle[sizeField])) = WorldScale.X * (size1 + (size2 - size1) * lerp);
            }
        }

    }
}
