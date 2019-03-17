// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public class HingeConstraint : Constraint
    {
        /// <summary>
        /// Sets the frames.
        /// </summary>
        /// <param name="frameA">The frame a.</param>
        /// <param name="frameB">The frame b.</param>
        public void SetFrames(Matrix frameA, Matrix frameB)
        {
            InternalHingeConstraint.SetFrames(frameA, frameB);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [angular only].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [angular only]; otherwise, <c>false</c>.
        /// </value>
        public bool AngularOnly
        {
            get { return InternalHingeConstraint.AngularOnly; }
            set { InternalHingeConstraint.AngularOnly = value; }
        }

        /// <summary>
        /// Gets the hinge angle.
        /// </summary>
        /// <value>
        /// The hinge angle.
        /// </value>
        public float HingeAngle
        {
            get { return InternalHingeConstraint.HingeAngle; }
        }

        /// <summary>
        /// Gets or sets the maximum motor impulse.
        /// </summary>
        /// <value>
        /// The maximum motor impulse.
        /// </value>
        public float MaxMotorImpulse
        {
            get { return InternalHingeConstraint.MaxMotorImpulse; }
            set { InternalHingeConstraint.MaxMotorImpulse = value; }
        }

        /// <summary>
        /// Gets the motor target velocity.
        /// </summary>
        /// <value>
        /// The motor target velocity.
        /// </value>
        public float MotorTargetVelocity
        {
            get { return InternalHingeConstraint.MotorTargetVelocity; }
        }

        /// <summary>
        /// Gets the solve limit.
        /// </summary>
        /// <value>
        /// The solve limit.
        /// </value>
        public int SolveLimit
        {
            get { return InternalHingeConstraint.SolveLimit; }
        }

        /// <summary>
        /// Gets the lower limit.
        /// </summary>
        /// <value>
        /// The lower limit.
        /// </value>
        public float LowerLimit
        {
            get { return InternalHingeConstraint.LowerLimit; }
        }

        /// <summary>
        /// Gets the upper limit.
        /// </summary>
        /// <value>
        /// The upper limit.
        /// </value>
        public float UpperLimit
        {
            get { return InternalHingeConstraint.UpperLimit; }
        }

        /// <summary>
        /// Gets the limit sign.
        /// </summary>
        /// <value>
        /// The limit sign.
        /// </value>
        public float LimitSign
        {
            get { return InternalHingeConstraint.LimitSign; }
        }

        /// <summary>
        /// Sets the limit.
        /// </summary>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        public void SetLimit(float low, float high)
        {
            InternalHingeConstraint.SetLimit(low, high);
        }

        /// <summary>
        /// Sets the limit.
        /// </summary>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        /// <param name="softness">The softness.</param>
        public void SetLimit(float low, float high, float softness)
        {
            InternalHingeConstraint.SetLimit(low, high, softness);
        }

        /// <summary>
        /// Sets the limit.
        /// </summary>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        /// <param name="softness">The softness.</param>
        /// <param name="biasFactor">The bias factor.</param>
        public void SetLimit(float low, float high, float softness, float biasFactor)
        {
            InternalHingeConstraint.SetLimit(low, high, softness, biasFactor);
        }

        /// <summary>
        /// Sets the limit.
        /// </summary>
        /// <param name="low">The low.</param>
        /// <param name="high">The high.</param>
        /// <param name="softness">The softness.</param>
        /// <param name="biasFactor">The bias factor.</param>
        /// <param name="relaxationFactor">The relaxation factor.</param>
        public void SetLimit(float low, float high, float softness, float biasFactor, float relaxationFactor)
        {
            InternalHingeConstraint.SetLimit(low, high, softness, biasFactor, relaxationFactor);
        }

        /// <summary>
        /// Enables the angular motor.
        /// </summary>
        /// <param name="enableMotor">if set to <c>true</c> [enable motor].</param>
        /// <param name="targetVelocity">The target velocity.</param>
        /// <param name="maxMotorImpulse">The maximum motor impulse.</param>
        public void EnableAngularMotor(bool enableMotor, float targetVelocity, float maxMotorImpulse)
        {
            InternalHingeConstraint.EnableAngularMotor(enableMotor, targetVelocity, maxMotorImpulse);
        }

        /// <summary>
        /// Enables the motor.
        /// </summary>
        /// <param name="enableMotor">if set to <c>true</c> [enable motor].</param>
        public void EnableMotor(bool enableMotor)
        {
            InternalHingeConstraint.EnableMotor = enableMotor;
        }

        /// <summary>
        /// Sets the motor target.
        /// </summary>
        /// <param name="targetAngle">The target angle.</param>
        /// <param name="dt">The dt.</param>
        public void SetMotorTarget(float targetAngle, float dt)
        {
            InternalHingeConstraint.SetMotorTarget(targetAngle, dt);
        }

        /// <summary>
        /// Sets the motor target.
        /// </summary>
        /// <param name="qAinB">The q ain b.</param>
        /// <param name="dt">The dt.</param>
        public void SetMotorTarget(Quaternion qAinB, float dt)
        {
            InternalHingeConstraint.SetMotorTarget(qAinB, dt);
        }

        internal BulletSharp.HingeConstraint InternalHingeConstraint;
    }
}
