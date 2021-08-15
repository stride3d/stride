// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    /// <summary>
    /// Description of a <see cref="SliderConstraint"/>.
    /// </summary>
    [Display("Slider")]
    [DataContract(nameof(SliderConstraintDesc))]
    public class SliderConstraintDesc : IConstraintDesc
    {
        /// <inheritdoc/>
        public ConstraintTypes Type => ConstraintTypes.Slider;

        /// <inheritdoc/>
        /// <remarks>Not used for the gear constraint.</remarks>
        [Display(0)]
        public Vector3 PivotInA { get; set; }

        /// <inheritdoc/>
        /// <remarks>Not used for the gear constraint.</remarks>
        [Display(1)]
        public Vector3 PivotInB { get; set; }

        /// <summary>
        /// Axis on which the gear will rotate relative to body A.
        /// </summary>
        /// <userdoc>
        /// Axis on which the gear will rotate relative to body A.
        /// </userdoc>
        [Display(2)]
        public Quaternion AxisInA { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Axis on which the gear will rotate relative to body B.
        /// </summary>
        /// <userdoc>
        /// Axis on which the gear will rotate relative to body B.
        /// </userdoc>
        [Display(3)]
        public Quaternion AxisInB { get; set; } = Quaternion.Identity;

        /// <userdoc>
        /// Limits properties.
        /// </userdoc>
        [Display(4)]
        public LimitDesc Limit { get; set; } = new LimitDesc();

        /// <inheritdoc/>
        public Constraint Build(RigidbodyComponent bodyA, RigidbodyComponent bodyB)
        {
            var frameA = Matrix.RotationQuaternion(AxisInA) * Matrix.Translation(PivotInA);
            var frameB = Matrix.RotationQuaternion(AxisInB) * Matrix.Translation(PivotInB);
            
            var slider = (bodyB == null
                ? Simulation.CreateConstraint(ConstraintTypes.Slider, bodyA, frameA)
                : Simulation.CreateConstraint(ConstraintTypes.Slider, bodyA, bodyB, frameA, frameB)
                ) as SliderConstraint;

            slider.LowerLinearLimit = Limit.LowerLinearLimit;
            slider.UpperLinearLimit = Limit.UpperLinearLimit;
            slider.LowerAngularLimit = Limit.LowerAngularLimit;
            slider.UpperAngularLimit = Limit.UpperAngularLimit;

            return slider;
        }

        /// <summary>
        /// Slider constraint properties regarding limits.
        /// </summary>
        [DataContract]
        public class LimitDesc
        {
            /// <summary>
            /// Lower linear limit along the constraint axis.
            /// </summary>
            /// <remarks>If Lower = Upper, the axis is locked; if Lower &gt; Upper, the axis is free; if Lower &lt; Upper, axis is limited in the range.</remarks>
            /// <userdoc>
            /// Lower linear limit along the constraint axis. If greater than upper limit, the axis is unconstrained.
            /// </userdoc>
            [Display(0)]
            public float LowerLinearLimit { get; set; } = 1;

            /// <summary>
            /// Upper linear limit along the constraint axis.
            /// </summary>
            /// <remarks>If Lower = Upper, the axis is locked; if Lower &gt; Upper, the axis is free; if Lower &lt; Upper, axis is limited in the range.</remarks>
            /// <userdoc>
            /// Upper linear limit along the constraint axis. If less than lower limit, the axis is unconstrained.
            /// </userdoc>
            [Display(1)]
            public float UpperLinearLimit { get; set; } = -1;

            /// <summary>
            /// Negative limit (-Pi, 0). Left handed rotation when thumb points at positive X axis of the constraint.
            /// </summary>
            /// <userdoc>
            /// Negative limit (-Pi, 0), where 0 is at positive Z axis. Left handed rotation when thumb points at positive X axis of the constraint.
            /// </userdoc>
            [Display(2)]
            [DataMemberRange(-Math.PI, 0, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
            public float LowerAngularLimit { get; set; } = 0;

            /// <summary>
            /// Positive limit (0, Pi). Right handed rotation when thumb points at positive X axis of the constraint.
            /// </summary>
            /// <userdoc>
            /// Positive limit (0, Pi), where 0 is at positive Z axis. Right handed rotation when thumb points at positive X axis of the constraint.
            /// </userdoc>
            [Display(3)]
            [DataMemberRange(0, Math.PI, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
            public float UpperAngularLimit { get; set; } = 0;
        }
    }
}
