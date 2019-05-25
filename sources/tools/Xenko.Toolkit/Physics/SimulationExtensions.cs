using Xenko.Core.Collections;
using Xenko.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Toolkit.Mathematics;

namespace Xenko.Toolkit.Physics
{
    /// <summary>
    /// Extensions for <see cref="SimulationExtensions"/>
    /// </summary>
    public static class SimulationExtensions
    {
        /// <summary>
        /// Raycasts and stops at the first hit.
        /// </summary>
        /// <param name="simulation">Physics simulation.</param>
        /// <param name="raySegment">Ray.</param>
        /// <returns>The hit results.</returns>
        /// <exception cref="ArgumentNullException">If the simulation argument is null.</exception>
        public static HitResult Raycast(this Simulation simulation, RaySegment raySegment)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            return simulation.Raycast(raySegment.Start, raySegment.End);
        }

        /// <summary>
        /// Raycasts and stops at the first hit.
        /// </summary>
        /// <param name="simulation">Physics simulation.</param>
        /// <param name="raySegment">Ray.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <returns>The hit results.</returns>
        /// <exception cref="ArgumentNullException">If the simulation argument is null.</exception>
        public static HitResult Raycast(this Simulation simulation, RaySegment raySegment, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            return simulation.Raycast(raySegment.Start, raySegment.End, collisionFilterGroups, collisionFilterGroupFlags);
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// </summary>
        /// <param name="simulation">Physics simulation.</param>
        /// <param name="raySegment">Ray.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <exception cref="ArgumentNullException">If the simulation argument is null.</exception>
        public static void RaycastPenetrating(this Simulation simulation, RaySegment raySegment, IList<HitResult> resultsOutput)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            simulation.RaycastPenetrating(raySegment.Start, raySegment.End, resultsOutput);
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// </summary>
        /// <param name="simulation">Physics simulation.</param>
        /// <param name="raySegment">Ray.</param>
        /// <returns>The list with hit results.</returns>
        /// <exception cref="ArgumentNullException">If the simulation argument is null.</exception>
        public static FastList<HitResult> RaycastPenetrating(this Simulation simulation, RaySegment raySegment)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            return simulation.RaycastPenetrating(raySegment.Start, raySegment.End);
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// Filtering by CollisionGroup
        /// </summary>
        /// <param name="simulation">Physics simulation.</param>
        /// <param name="raySegment">Ray.</param>
        /// <param name="resultsOutput">The list to fill with results.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <exception cref="ArgumentNullException">If the simulation argument is null.</exception>
        public static void RaycastPenetrating(this Simulation simulation, RaySegment raySegment, IList<HitResult> resultsOutput, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            simulation.RaycastPenetrating(raySegment.Start, raySegment.End, resultsOutput, collisionFilterGroups, collisionFilterGroupFlags);
        }

        /// <summary>
        /// Raycasts penetrating any shape the ray encounters.
        /// </summary>
        /// <param name="simulation">Physics simulation.</param>
        /// <param name="raySegment">Ray.</param>
        /// <param name="collisionFilterGroups">The collision group of this shape sweep</param>
        /// <param name="collisionFilterGroupFlags">The collision group that this shape sweep can collide with</param>
        /// <returns>The list with hit results.</returns>
        /// <exception cref="ArgumentNullException">If the simulation argument is null.</exception>
        public static FastList<HitResult> RaycastPenetrating(this Simulation simulation, RaySegment raySegment, CollisionFilterGroups collisionFilterGroups, CollisionFilterGroupFlags collisionFilterGroupFlags)
        {
            if (simulation == null)
            {
                throw new ArgumentNullException(nameof(simulation));
            }

            return simulation.RaycastPenetrating(raySegment, collisionFilterGroups, collisionFilterGroupFlags);
        }
    }
}
