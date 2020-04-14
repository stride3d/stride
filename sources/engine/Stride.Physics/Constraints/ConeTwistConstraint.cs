// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Physics
{
    public class ConeTwistConstraint : Constraint
    {
        internal BulletSharp.ConeTwistConstraint InternalConeTwistConstraint;

        /// <summary>
        ///     Gets or sets the fix thresh.
        /// </summary>
        /// <value>
        ///     The fix thresh.
        /// </value>
        public float FixThresh
        {
            get { return InternalConeTwistConstraint.FixThresh; }
            set { InternalConeTwistConstraint.FixThresh = value; }
        }

        /// <summary>
        ///     Gets the swing span1.
        /// </summary>
        /// <value>
        ///     The swing span1.
        /// </value>
        public float SwingSpan1 => InternalConeTwistConstraint.SwingSpan1;

        /// <summary>
        ///     Gets the swing span2.
        /// </summary>
        /// <value>
        ///     The swing span2.
        /// </value>
        public float SwingSpan2 => InternalConeTwistConstraint.SwingSpan2;

        /// <summary>
        ///     Gets the twist angle.
        /// </summary>
        /// <value>
        ///     The twist angle.
        /// </value>
        public float TwistAngle => InternalConeTwistConstraint.TwistAngle;

        /// <summary>
        ///     Gets the twist limit sign.
        /// </summary>
        /// <value>
        ///     The twist limit sign.
        /// </value>
        public float TwistLimitSign => InternalConeTwistConstraint.TwistLimitSign;

        /// <summary>
        ///     Gets the twist span.
        /// </summary>
        /// <value>
        ///     The twist span.
        /// </value>
        public float TwistSpan => InternalConeTwistConstraint.TwistSpan;

        /// <summary>
        ///     Gets a value indicating whether this instance is past swing limit.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is past swing limit; otherwise, <c>false</c>.
        /// </value>
        public bool IsPastSwingLimit => InternalConeTwistConstraint.IsPastSwingLimit;

        /// <summary>
        ///     Gets the solve swing limit.
        /// </summary>
        /// <value>
        ///     The solve swing limit.
        /// </value>
        public int SolveSwingLimit => InternalConeTwistConstraint.SolveSwingLimit;

        /// <summary>
        ///     Gets the solve twist limit.
        /// </summary>
        /// <value>
        ///     The solve twist limit.
        /// </value>
        public int SolveTwistLimit => InternalConeTwistConstraint.SolveTwistLimit;

        /// <summary>
        ///     Sets the frames.
        /// </summary>
        /// <param name="frameA">The frame a.</param>
        /// <param name="frameB">The frame b.</param>
        public void SetFrames(Matrix frameA, Matrix frameB)
        {
            InternalConeTwistConstraint.SetFrames(frameA, frameB);
        }

        /// <summary>
        ///     Sets the limit.
        /// </summary>
        /// <param name="swingSpan1">The swing span1.</param>
        /// <param name="swingSpan2">The swing span2.</param>
        /// <param name="twistSpan">The twist span.</param>
        public void SetLimit(float swingSpan1, float swingSpan2, float twistSpan)
        {
            InternalConeTwistConstraint.SetLimit(swingSpan1, swingSpan2, twistSpan);
        }

        /// <summary>
        ///     Sets the limit.
        /// </summary>
        /// <param name="swingSpan1">The swing span1.</param>
        /// <param name="swingSpan2">The swing span2.</param>
        /// <param name="twistSpan">The twist span.</param>
        /// <param name="softness">The softness.</param>
        /// <param name="biasFactor">The bias factor.</param>
        public void SetLimit(float swingSpan1, float swingSpan2, float twistSpan, float softness, float biasFactor)
        {
            InternalConeTwistConstraint.SetLimit(swingSpan1, swingSpan2, twistSpan, softness);
        }

        /// <summary>
        ///     Sets the limit.
        /// </summary>
        /// <param name="swingSpan1">The swing span1.</param>
        /// <param name="swingSpan2">The swing span2.</param>
        /// <param name="twistSpan">The twist span.</param>
        /// <param name="softness">The softness.</param>
        /// <param name="biasFactor">The bias factor.</param>
        /// <param name="relaxationFactor">The relaxation factor.</param>
        public void SetLimit(float swingSpan1, float swingSpan2, float twistSpan, float softness, float biasFactor, float relaxationFactor)
        {
            InternalConeTwistConstraint.SetLimit(swingSpan1, swingSpan2, twistSpan, softness, biasFactor);
        }

        /// <summary>
        ///     Sets the angular only.
        /// </summary>
        /// <param name="angularOnly">if set to <c>true</c> [angular only].</param>
        public void SetAngularOnly(bool angularOnly)
        {
            InternalConeTwistConstraint.AngularOnly = angularOnly;
        }

        /// <summary>
        ///     Sets the damping.
        /// </summary>
        /// <param name="damping">The damping.</param>
        public void SetDamping(float damping)
        {
            InternalConeTwistConstraint.Damping = damping;
        }

        /// <summary>
        ///     Enables the motor.
        /// </summary>
        /// <param name="b">if set to <c>true</c> [b].</param>
        public void EnableMotor(bool b)
        {
            InternalConeTwistConstraint.EnableMotor(b);
        }

        /// <summary>
        ///     Sets the maximum motor impulse.
        /// </summary>
        /// <param name="maxMotorImpulse">The maximum motor impulse.</param>
        public void SetMaxMotorImpulse(float maxMotorImpulse)
        {
            InternalConeTwistConstraint.MaxMotorImpulse = maxMotorImpulse;
        }

        /// <summary>
        ///     Sets the maximum motor impulse normalized.
        /// </summary>
        /// <param name="maxMotorImpulse">The maximum motor impulse.</param>
        public void SetMaxMotorImpulseNormalized(float maxMotorImpulse)
        {
            InternalConeTwistConstraint.SetMaxMotorImpulseNormalized(maxMotorImpulse);
        }

        /// <summary>
        ///     Sets the motor target.
        /// </summary>
        /// <param name="q">The q.</param>
        public void SetMotorTarget(Quaternion q)
        {
            InternalConeTwistConstraint.MotorTarget = q;
        }

        /// <summary>
        ///     Sets the motor target in constraint space.
        /// </summary>
        /// <param name="q">The q.</param>
        public void SetMotorTargetInConstraintSpace(Quaternion q)
        {
            InternalConeTwistConstraint.SetMotorTargetInConstraintSpace(q);
        }
    }
}
