// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    /// <summary>
    /// Description of a <see cref="ConeTwistConstraint"/>.
    /// </summary>
    [Display("Cone Twist")]
    [DataContract(nameof(ConeTwistConstraintDesc))]
    public class ConeTwistConstraintDesc : IConstraintDesc
    {
        /// <inheritdoc/>
        public ConstraintTypes Type => ConstraintTypes.ConeTwist;

        /// <inheritdoc/>
        /// <userdoc>
        /// Position local to rigidbody A.
        /// </userdoc>
        [Display(0)]
        public Vector3 PivotInA { get; set; }

        /// <inheritdoc/>
        /// <userdoc>
        /// Position local to rigidbody B. Ignored when creating body-world constraint.
        /// </userdoc>
        [Display(1)]
        public Vector3 PivotInB { get; set; }

        /// <summary>
        /// Axis on which the cone will twist relative to body A.
        /// </summary>
        /// <userdoc>
        /// Axis on which the cone will twist relative to body A.
        /// </userdoc>
        [Display(2)]
        public Quaternion AxisInA { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Axis on which the cone will twist relative to body B.
        /// </summary>
        /// <userdoc>
        /// Axis on which the cone will twist relative to body B.
        /// </userdoc>
        [Display(3)]
        public Quaternion AxisInB { get; set; } = Quaternion.Identity;

        /// <summary>
        /// If true there will be a limit set on the constraint.
        /// </summary>
        /// <userdoc>
        /// Wheather there should be limits set on the constraint.
        /// </userdoc>
        [Display(5)]
        public bool SetLimit { get; set; }

        /// <summary>
        /// Limit on the swing in the direction of the constraint Z axis.
        /// </summary>
        /// <userdoc>
        /// Limit on the swing in the direction of the constraint Z axis.
        /// </userdoc>
        [Display(6)]
        [DataMemberRange(0, Math.PI, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
        public float SwingSpanLimitZ { get; set; } = (float)Math.PI;

        /// <summary>
        /// Limit on the swing in the direction of the constraint Y axis.
        /// </summary>
        /// <userdoc>
        /// Limit on the swing in the direction of the constraint Y axis.
        /// </userdoc>
        [Display(7)]
        [DataMemberRange(0, Math.PI, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
        public float SwingSpanLimitY { get; set; } = (float)Math.PI;

        /// <summary>
        /// Limit on the twist (rotation around constraint axis).
        /// </summary>
        /// <userdoc>
        /// Limit on the twist (rotation around constraint X axis).
        /// </userdoc>
        [Display(8)]
        [DataMemberRange(0, Math.PI, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
        public float TwistSpanLimit { get; set; } = (float)Math.PI;

        public Constraint Build(RigidbodyComponent bodyA, RigidbodyComponent bodyB)
        {
            var frameA = Matrix.RotationQuaternion(AxisInA) * Matrix.Translation(PivotInA);
            var frameB = Matrix.RotationQuaternion(AxisInB) * Matrix.Translation(PivotInB);

            var coneTwist = (bodyB == null
                ? Simulation.CreateConstraint(
                    ConstraintTypes.ConeTwist,
                    bodyA,
                    frameA)
                : Simulation.CreateConstraint(
                    ConstraintTypes.ConeTwist,
                    bodyA,
                    bodyB,
                    frameA,
                    frameB)) as ConeTwistConstraint;

            if (SetLimit)
                coneTwist.SetLimit(SwingSpanLimitY, SwingSpanLimitZ, TwistSpanLimit);

            return coneTwist;
        }
    }
}
