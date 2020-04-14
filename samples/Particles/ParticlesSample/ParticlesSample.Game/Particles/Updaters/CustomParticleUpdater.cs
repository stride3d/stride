// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Particles;
using Stride.Particles.Modules;

namespace ParticlesSample.Particles.Updaters
{
    public enum AnimatedCurveEnum
    {
        [Display("Cos(time)")]
        CosCos,

        [Display("Sin(time)")]
        SinSin,

        [Display("Cos/Sin")]
        CosSin,

        [Display("Sin/Cos")]
        SinCos,
    }

    [DataContract("CustomParticleUpdater")] // Used for serialization, a good practice is to have the data contract have the same name as the class
    [Display("CustomUpdater")]
    public class CustomParticleUpdater : ParticleUpdater
    {
        // By making this updater a post-updater we can ensure it will be called for both newly spawned and old particles (1 frame or older)
        [DataMemberIgnore]
        public override bool IsPostUpdater
        {
            get { return true; }
        }

        /// <summary>
        /// We use a simple matechematical function to calculate the rectangle's dimensions based on the particle's lifetime
        /// </summary>
        [DataMember(10)]
        public AnimatedCurveEnum Curve;

        public CustomParticleUpdater()
        {
            // This is going to be our "input" field
            RequiredFields.Add(ParticleFields.Life);

            // This is the field we want to update
            // It is not part of the basic fields - we created it just for this updater
            RequiredFields.Add(CustomParticleFields.RectangleXY);
        }

        /// <summary>
        /// The Update(...) step is called every frame for the particle pool, containing all particles
        /// Since this is a post-updater the Update(...) step is called *after* new particles have spawned for this frame
        /// Regular updaters are invoked *before* spawning new particles and avoid updating the particles on the frame they spawn
        /// </summary>
        public override void Update(float dt, ParticlePool pool)
        {
            // Make sure the fields we require exist, otherwise trying to access them will result in an invalid memory
            // The particle pool can check if the accessor is valid on each access, but that would be very inefficient so it skips the check
            if (!pool.FieldExists(ParticleFields.Life) || !pool.FieldExists(CustomParticleFields.RectangleXY))
                return;

            var lifeField = pool.GetField(ParticleFields.Life);
            var rectField = pool.GetField(CustomParticleFields.RectangleXY);

            // Instead of switching for each particle, we switch only once and then batch-update the particles to improve performance
            switch (Curve)
            {
                // X and Y sides both depend on cos(time)
                case AnimatedCurveEnum.CosCos:
                    {
                        foreach (var particle in pool)
                        {
                            // Get the particle's remaining life. It's normalized between 0 and 1
                            var lifePi = particle.Get(lifeField) * MathUtil.Pi;

                            // Set the rectangle as a simple function over time
                            particle.Set(rectField, new Vector2((float)Math.Cos(lifePi), (float)Math.Cos(lifePi)));
                        }
                    }
                    break;


                // X and Y sides both depend on sin(time)
                case AnimatedCurveEnum.SinSin:
                    {
                        foreach (var particle in pool)
                        {
                            // Get the particle's remaining life. It's normalized between 0 and 1
                            var lifePi = particle.Get(lifeField) * MathUtil.Pi;

                            // Set the rectangle as a simple function over time
                            particle.Set(rectField, new Vector2((float)Math.Sin(lifePi), (float)Math.Sin(lifePi)));
                        }
                    }
                    break;


                // X and Y sides depend on cos(time) and sin(time)
                case AnimatedCurveEnum.CosSin:
                    {
                        foreach (var particle in pool)
                        {
                            // Get the particle's remaining life. It's normalized between 0 and 1
                            var lifePi = particle.Get(lifeField) * MathUtil.Pi;

                            // Set the rectangle as a simple function over time
                            particle.Set(rectField, new Vector2((float)Math.Cos(lifePi), (float)Math.Sin(lifePi)));
                        }
                    }
                    break;


                // X and Y sides depend on sin(time) and cos(time)
                case AnimatedCurveEnum.SinCos:
                    {
                        foreach (var particle in pool)
                        {
                            // Get the particle's remaining life. It's normalized between 0 and 1
                            var lifePi = particle.Get(lifeField) * MathUtil.Pi;

                            // Set the rectangle as a simple function over time
                            particle.Set(rectField, new Vector2((float)Math.Sin(lifePi), (float)Math.Cos(lifePi)));
                        }
                    }
                    break;
            }
        }
    }
}
