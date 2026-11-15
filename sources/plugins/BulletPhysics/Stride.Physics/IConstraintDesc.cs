// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Physics
{
    /// <summary>
    /// Description of a <see cref="Constraint"/>.
    /// </summary>
    public interface IConstraintDesc
    {
        /// <summary>
        /// Type of the constraint description.
        /// </summary>
        ConstraintTypes Type { get; }

        /// <summary>
        /// Position local to rigidbody A.
        /// </summary>
        Vector3 PivotInA { get; }

        /// <summary>
        /// Position local to rigidbody B.
        /// </summary>
        /// <remarks>
        /// Ignored when creating a body-world constraint.
        /// </remarks>
        Vector3 PivotInB { get; }

        /// <summary>
        /// Create a new constraint according to the description properties between bodies A and B, or between A and World.
        /// </summary>
        /// <param name="rigidbodyA">Rigidbody A.</param>
        /// <param name="rigidbodyB">Rigidbody B (may be null).</param>
        /// <returns>
        /// A new constraint constructed in the <see cref="Simulation"/>.
        /// Needs to be added with <see cref="Simulation.AddConstraint(Constraint)"/> to take effect.
        /// </returns>
        Constraint Build(RigidbodyComponent rigidbodyA, RigidbodyComponent rigidbodyB);
    }
}
