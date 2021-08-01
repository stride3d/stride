// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    /// <summary>
    /// Description of a <see cref="Point2PointConstraint"/>.
    /// </summary>
    [Display("Point to Point")]
    [DataContract(nameof(Point2PointConstraintDesc))]
    public class Point2PointConstraintDesc : IConstraintDesc
    {
        /// <inheritdoc/>
        public ConstraintTypes Type => ConstraintTypes.Point2Point;

        /// <inheritdoc/>
        /// <userdoc>
        /// Position local to rigidbody A.
        /// </userdoc>
        public Vector3 PivotInA { get; set; }

        /// <inheritdoc/>
        /// <userdoc>
        /// Position local to rigidbody B. Ignored when creating body-world constraint.
        /// </userdoc>
        public Vector3 PivotInB { get; set; }

        public Constraint Build(RigidbodyComponent bodyA, RigidbodyComponent bodyB)
        {
            var frameA = Matrix.Translation(PivotInA);
            var frameB = Matrix.Translation(PivotInB);

            var point2point = (bodyB == null
                ? Simulation.CreateConstraint(
                    ConstraintTypes.Point2Point,
                    bodyA,
                    frameA)
                : Simulation.CreateConstraint(
                    ConstraintTypes.Point2Point,
                    bodyA,
                    bodyB,
                    frameA,
                    frameB)) as Point2PointConstraint;

            return point2point;
        }
    }
}
