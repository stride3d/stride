// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    /// <summary>
    /// Description of a <see cref="GearConstraint"/>.
    /// </summary>
    [Display("Gear")]
    [DataContract(nameof(GearConstraintDesc))]
    public class GearConstraintDesc : IConstraintDesc
    {
        /// <inheritdoc/>
        public ConstraintTypes Type => ConstraintTypes.Gear;

        /// <inheritdoc/>
        /// <remarks>Not used for the gear constraint.</remarks>
        public Vector3 PivotInA => Vector3.Zero;

        /// <inheritdoc/>
        /// <remarks>Not used for the gear constraint.</remarks>
        public Vector3 PivotInB => Vector3.Zero;

        /// <summary>
        /// Axis on which the gear will rotate relative to body A.
        /// </summary>
        /// <userdoc>
        /// Axis on which the gear will rotate relative to body A.
        /// </userdoc>
        [Display(0)]
        public Quaternion AxisInA { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Axis on which the gear will rotate relative to body B.
        /// </summary>
        /// <userdoc>
        /// Axis on which the gear will rotate relative to body B.
        /// </userdoc>
        [Display(1)]
        public Quaternion AxisInB { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Size ratio between the gears (rotating a bigger gear will rotate smaller gear quicker).
        /// </summary>
        /// <userdoc>
        /// Size ratio between the gears (rotating a bigger gear will rotate smaller gear quicker).
        /// </userdoc>
        [Display(2)]
        public float Ratio { get; set; } = 1;

        /// <inheritdoc/>
        public Constraint Build(RigidbodyComponent bodyA, RigidbodyComponent bodyB)
        {
            if (bodyB == null) throw new System.InvalidOperationException("A Gear constraint requires two rigidbodies.");

            var axis = Vector3.UnitX;
            AxisInA.Rotate(ref axis);
            var frameA = Matrix.Translation(axis);

            axis = Vector3.UnitX;
            AxisInB.Rotate(ref axis);
            var frameB = Matrix.Translation(axis);

            var gear = Simulation.CreateConstraint(ConstraintTypes.Gear, bodyA, bodyB, frameA, frameB) as GearConstraint;

            gear.Ratio = Ratio;

            return gear;
        }
    }
}
