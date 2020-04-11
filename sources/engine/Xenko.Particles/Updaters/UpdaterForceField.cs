// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;
using Xenko.Particles.Updaters.FieldShapes;

namespace Xenko.Particles.Modules
{
    /// <summary>
    /// The <see cref="UpdaterForceField"/> updates the particles' positions and velocity based on proximity and relative position to a bounding force field
    /// </summary>
    [DataContract("UpdaterForceField")]
    [Display("Force Field")]
    public class UpdaterForceField : ParticleUpdater
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public UpdaterForceField()
        {
            // A force field operates over the particle's position and velocity, updating them as required
            RequiredFields.Add(ParticleFields.Position);
            RequiredFields.Add(ParticleFields.Velocity);

            DisplayParticlePosition = true;
            DisplayParticleRotation = true;
            DisplayParticleScale = true;
        }

        /// <summary>
        /// Shows how much the force vector should scale when the bounding box also scales
        /// </summary>
        [DataMemberIgnore]
        private float parentScale = 1f;

        /// <summary>
        /// The shape defines the force field's bounding shape, which influences the force vectors and magnitude for every given particle
        /// </summary>
        /// <userdoc>
        /// The shape defines the force field's bounding shape, which influences the force vectors and magnitude for every given particle
        /// </userdoc>
        [DataMember(10)]
        [Display("Shape")]
        public FieldShape FieldShape { get; set; }

        /// <summary>
        /// Defines how and if the total magnitude of the force should change depending of how far away the particle is from the central axis
        /// </summary>
        /// <userdoc>
        /// Defines how and if the total magnitude of the force should change depending of how far away the particle is from the central axis
        /// </userdoc>
        [DataMember(40)]
        [Display("Falloff")]
        public FieldFalloff FieldFalloff { get; set; } = new FieldFalloff();

        /// <summary>
        /// How much of the force should be applied as conserved energy (acceleration)
        /// </summary>
        /// <userdoc>
        /// With no concervation (0) particles will cease to move when the force disappears (physically incorrect, but easier to control).
        /// With energy concervation (1) particles will retain energy and gradually accelerate, continuing to move even when the force
        /// cease to exist (physically correct, but more difficult to control).
        /// </userdoc>
        [DataMember(50)]
        [DataMemberRange(0, 1, 0.01, 0.1, 3)]
        [Display("Energy conservation")]
        public float EnergyConservation { get; set; } = 0f;

        /// <summary>
        /// The force ALONG the bounding shape's axis.
        /// </summary>
        /// <userdoc>
        /// The force ALONG the bounding shape's axis.
        /// </userdoc>
        [DataMember(60)]
        [Display("Directed force")]
        public float ForceDirected { get; set; }

        /// <summary>
        /// The force AROUND the bounding shape's axis.
        /// </summary>
        /// <userdoc>
        /// The force AROUND the bounding shape's axis.
        /// </userdoc>
        [DataMember(70)]
        [Display("Vortex force")]
        public float ForceVortex { get; set; } = 1f;

        /// <summary>
        /// The force AWAY from the bounding shape's axis.
        /// </summary>
        /// <userdoc>
        /// The force AWAY from the bounding shape's axis.
        /// </userdoc>
        [DataMember(80)]
        [Display("Repulsive force")]
        public float ForceRepulsive { get; set; } = 1f;

        /// <summary>
        /// The fixed force doesn't scale or rotate with the the bounding shape
        /// </summary>
        /// <userdoc>
        /// The fixed force doesn't scale or rotate with the the bounding shape
        /// </userdoc>
        [DataMember(100)]
        [Display("Fixed force")]
        public Vector3 ForceFixed { get; set; } = Vector3.Zero;

        /// <inheritdoc />
        public override unsafe void Update(float dt, ParticlePool pool)
        {
            // The force field operates over position and velocity. If the particles don't have such fields we can't run this update
            if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.Velocity))
                return;

            var posField = pool.GetField(ParticleFields.Position);
            var velField = pool.GetField(ParticleFields.Velocity);

            // Depending on our settings some of the energy will be lost (it directly translates to changes in position)
            //  and some of the energy will be preserved (it translates to changes in velocity)
            var directToPosition = 1f - EnergyConservation;

            foreach (var particle in pool)
            {
                var alongAxis  = new Vector3(0, 1, 0);
                var awayAxis   = new Vector3(0, 0, 1);
                var aroundAxis = new Vector3(1, 0, 0);

                var particlePos = (*((Vector3*)particle[posField]));
                var particleVel = (*((Vector3*)particle[velField]));

                var forceMagnitude = 1f;
                if (FieldShape != null)
                {
                    FieldShape.PreUpdateField(WorldPosition, WorldRotation, WorldScale);

                    forceMagnitude = FieldShape.GetDistanceToCenter(particlePos, particleVel, out alongAxis, out aroundAxis, out awayAxis);

                    forceMagnitude = FieldFalloff.GetStrength(forceMagnitude);

                }
                forceMagnitude *= dt * parentScale;

                var totalForceVector = ForceFixed + 
                    alongAxis  * ForceDirected +
                    aroundAxis * ForceVortex +
                    awayAxis * ForceRepulsive;

                totalForceVector *= forceMagnitude;
               
                // Force contribution to velocity - conserved energy
                var vectorContribution = totalForceVector * EnergyConservation;
                (*((Vector3*)particle[velField])) += vectorContribution;

                // Force contribution to position - lost energy
                vectorContribution = (vectorContribution * (dt * 0.5f)) + (totalForceVector * directToPosition);
                (*((Vector3*)particle[posField])) += vectorContribution;
            }
        }

        /// <inheritdoc />
        public override void SetParentTRS(ParticleTransform transform, ParticleSystem parent)
        {
            base.SetParentTRS(transform, parent);
            parentScale = (InheritScale) ? transform.WorldScale.X : 1f;
        }

        /// <summary>
        /// Should this Particle Module's bounds be displayed as a debug draw
        /// </summary>
        /// <userdoc>
        /// Display the Particle Module's bounds as a wireframe debug shape. Temporary feature (will be removed later)!
        /// </userdoc>
        [DataMember(-1)]
        [DefaultValue(false)]
        public bool DebugDraw { get; set; } = false;

        /// <inheritdoc />
        public override bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            if (!DebugDraw)
                return base.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale);

            rotation = Quaternion.Identity;
            scale = Vector3.One;
            translation = Vector3.Zero;

            debugDrawShape = FieldShape?.GetDebugDrawShape(out translation, out rotation, out scale) ?? DebugDrawShape.None;

            rotation *= WorldRotation;

            scale *= WorldScale;

            translation *= WorldScale;
            rotation.Rotate(ref translation);
            translation += WorldPosition;

            return true;
        }
    }
}
