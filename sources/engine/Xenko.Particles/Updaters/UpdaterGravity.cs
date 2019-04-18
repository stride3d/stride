// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Mathematics;

namespace Xenko.Particles.Modules
{
    /// <summary>
    /// The <see cref="UpdaterGravity"/> is a simple version of a force field which updates the particles' velocity over time and acts as a gravitational force
    /// </summary>
    [DataContract("UpdaterGravity")]
    [Display("Gravity")]
    public class UpdaterGravity : ParticleUpdater
    {
        /// <summary>
        /// Direction and magnitude of the gravitational acceleration
        /// </summary>
        [DataMember(9)]
        public Vector3 GravitationalAcceleration;

        public UpdaterGravity()
        {
            // In case of a conventional standard Earth gravitational acceleration, where Y is up. Change it depending on your needs.
            GravitationalAcceleration = new Vector3(0, -9.80665f, 0);

            RequiredFields.Add(ParticleFields.Position);
            RequiredFields.Add(ParticleFields.Velocity);
        }

        public override unsafe void Update(float dt, ParticlePool pool)
        {
            if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.Velocity))
                return;

            var posField = pool.GetField(ParticleFields.Position);
            var velField = pool.GetField(ParticleFields.Velocity);

            var deltaVel = GravitationalAcceleration * dt;
            var deltaPos = deltaVel * (dt * 0.5f);

            foreach (var particle in pool)
            {
                (*((Vector3*)particle[posField])) += deltaPos;
                (*((Vector3*)particle[velField])) += deltaVel;
            }
        }
    }
}
