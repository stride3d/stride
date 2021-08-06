// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;

namespace Stride.Physics.Constraints
{
    /// <summary>
    /// Description of a <see cref="HingeConstraint"/>.
    /// </summary>
    [Display("Hinge")]
    [DataContract(nameof(HingeConstraintDesc))]
    public class HingeConstraintDesc : IConstraintDesc
    {
        /// <inheritdoc/>
        public ConstraintTypes Type => ConstraintTypes.Hinge;

        /// <summary>
        /// Position local to rigidbody A.
        /// </summary>
        /// <userdoc>
        /// Position local to rigidbody A.
        /// </userdoc>
        [Display(0)]
        public Vector3 PivotInA { get; set; }

        /// <summary>
        /// Position local to rigidbody B.
        /// </summary>
        /// <remarks>
        /// Ignored when creating a body-world constraint.
        /// </remarks>
        /// <userdoc>
        /// Position local to rigidbody B. Ignored when creating body-world constraint.
        /// </userdoc>
        [Display(1)]
        public Vector3 PivotInB { get; set; }

        /// <summary>
        /// Axis on which the hinge will rotate relative to body A.
        /// </summary>
        /// <userdoc>
        /// Axis on which the hinge will rotate relative to body A.
        /// </userdoc>
        [Display(2)]
        public Quaternion AxisInA { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Axis on which the hinge will rotate relative to body B.
        /// </summary>
        /// <userdoc>
        /// Axis on which the hinge will rotate relative to body B.
        /// </userdoc>
        [Display(3)]
        public Quaternion AxisInB { get; set; } = Quaternion.Identity;

        /// <summary>
        /// Use Axis in relation to body A. <!-- TODO: this isn't completely accurate -->
        /// </summary>
        [Display(4)]
        public bool UseReferenceFrameA { get; set; }

        /// <userdoc>
        /// Limits properties.
        /// </userdoc>
        [Display(5)]
        public LimitDesc Limit { get; set; } = new LimitDesc();

        /// <userdoc>
        /// Motor properties.
        /// </userdoc>
        [Display(6)]
        public MotorDesc Motor { get; set; } = new MotorDesc();

        /// <inheritdoc/>
        public Constraint Build(RigidbodyComponent rigidbodyA, RigidbodyComponent rigidbodyB)
        {
            if (rigidbodyA == null)
                throw new ArgumentNullException(nameof(rigidbodyA));
            if (rigidbodyB != null && rigidbodyB.Simulation != rigidbodyA.Simulation)
                throw new Exception("Both RigidBodies must be on the same simulation");

            var rbA = rigidbodyA.InternalRigidBody;
            var rbB = rigidbodyB?.InternalRigidBody;
            var axisA = Vector3.UnitX;
            AxisInA.Rotate(ref axisA);
            var axisB = Vector3.UnitX;
            AxisInA.Rotate(ref axisB);

            HingeConstraint hinge = new HingeConstraint
            {
                InternalHingeConstraint =
                            rigidbodyB == null ?
                                new BulletSharp.HingeConstraint(rbA, PivotInA, axisA) :
                                new BulletSharp.HingeConstraint(rbA, rbB, PivotInA, PivotInB, axisA, axisB, UseReferenceFrameA),
            };
            hinge.InternalConstraint = hinge.InternalHingeConstraint;

            if (Limit.SetLimit)
            {
                hinge.SetLimit(Limit.LowerLimit, Limit.UpperLimit);
            }

            if (Motor.EnableMotor)
            {
                hinge.EnableAngularMotor(Motor.EnableMotor, Motor.TargetVelocity, Motor.MaxMotorImpulse);
            }

            return hinge;
        }

        /// <summary>
        /// Hinge constraint properties regarding limits.
        /// </summary>
        [DataContract]
        public class LimitDesc
        {
            /// <summary>
            /// If true there will be a limit set on the constraint.
            /// </summary>
            /// <remarks>
            /// The limits are angles determining the area of freedom for the constraint,
            /// calculated from 0 to ±PI, with 0 being at the positive Z axis of the constraint (with X being the hinge axis).
            /// </remarks>
            /// <userdoc>
            /// Whether there should be limits set on the constraint.
            /// </userdoc>
            [Display(0)]
            public bool SetLimit { get; set; }

            /// <summary>
            /// Negative limit (-Pi, 0). Left handed rotation when thumb points at positive X axis of the constraint.
            /// </summary>
            /// <userdoc>
            /// Negative limit (-Pi, 0), where 0 is at positive Z axis. Left handed rotation when thumb points at positive X axis of the constraint.
            /// </userdoc>
            [Display(1)]
            [DataMemberRange(-Math.PI, 0, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
            public float LowerLimit { get; set; } = -MathF.PI;

            /// <summary>
            /// Positive limit (0, Pi). Right handed rotation when thumb points at positive X axis of the constraint.
            /// </summary>
            /// <userdoc>
            /// Positive limit (0, Pi), where 0 is at positive Z axis. Right handed rotation when thumb points at positive X axis of the constraint.
            /// </userdoc>
            [Display(2)]
            [DataMemberRange(0, Math.PI, MathUtil.PiOverFour / 9, MathUtil.PiOverFour, 3)]
            public float UpperLimit { get; set; } = MathF.PI;
        }

        /// <summary>
        /// Hinge constraint properties regarding the angular motor.
        /// </summary>
        [DataContract]
        public struct MotorDesc
        {
            /// <summary>
            /// Enables an angular motor on the constraint.
            /// </summary>
            /// <userdoc>
            /// Enables an angular motor on the constraint.
            /// </userdoc>
            [Display(0)]
            public bool EnableMotor { get; set; }

            /// <summary>
            /// Target angular velocity of the motor.
            /// </summary>
            /// <userdoc>
            /// Target angular velocity of the motor.
            /// </userdoc>
            [Display(1)]
            public float TargetVelocity { get; set; }

            /// <summary>
            /// Maximum motor impulse.
            /// </summary>
            /// <userdoc>
            /// Maximum motor impulse.
            /// </userdoc>
            [Display(2)]
            public float MaxMotorImpulse { get; set; }
        }
    }
}
