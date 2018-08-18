// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public class TranslationalLimitMotor
    {
        private BulletSharp.TranslationalLimitMotor mMotor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationalLimitMotor"/> class.
        /// </summary>
        /// <param name="motor">The motor.</param>
        public TranslationalLimitMotor(BulletSharp.TranslationalLimitMotor motor)
        {
            mMotor = motor;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (mMotor == null) return;
            mMotor.Dispose();
            mMotor = null;
        }

        /// <summary>
        /// Gets or sets the accumulated impulse.
        /// </summary>
        /// <value>
        /// The accumulated impulse.
        /// </value>
        public Vector3 AccumulatedImpulse
        {
            get { return mMotor.AccumulatedImpulse; }
            set { mMotor.AccumulatedImpulse = value; }
        }

        /// <summary>
        /// Gets or sets the current limit error.
        /// </summary>
        /// <value>
        /// The current limit error.
        /// </value>
        public Vector3 CurrentLimitError
        {
            get { return mMotor.CurrentLimitError; }
            set { mMotor.CurrentLimitError = value; }
        }

        /// <summary>
        /// Gets or sets the current linear difference.
        /// </summary>
        /// <value>
        /// The current linear difference.
        /// </value>
        public Vector3 CurrentLinearDiff
        {
            get { return mMotor.CurrentLinearDiff; }
            set { mMotor.CurrentLinearDiff = value; }
        }

        /// <summary>
        /// Gets or sets the damping.
        /// </summary>
        /// <value>
        /// The damping.
        /// </value>
        public float Damping
        {
            get { return mMotor.Damping; }
            set { mMotor.Damping = value; }
        }

        /// <summary>
        /// Gets or sets the limit softness.
        /// </summary>
        /// <value>
        /// The limit softness.
        /// </value>
        public float LimitSoftness
        {
            get { return mMotor.LimitSoftness; }
            set { mMotor.LimitSoftness = value; }
        }

        /// <summary>
        /// Gets or sets the lower limit.
        /// </summary>
        /// <value>
        /// The lower limit.
        /// </value>
        public Vector3 LowerLimit
        {
            get { return mMotor.LowerLimit; }
            set { mMotor.LowerLimit = value; }
        }

        /// <summary>
        /// Gets or sets the maximum motor force.
        /// </summary>
        /// <value>
        /// The maximum motor force.
        /// </value>
        public Vector3 MaxMotorForce
        {
            get { return mMotor.MaxMotorForce; }
            set { mMotor.MaxMotorForce = value; }
        }

        /// <summary>
        /// Gets or sets the normal CFM.
        /// </summary>
        /// <value>
        /// The normal CFM.
        /// </value>
        public Vector3 NormalCFM
        {
            get { return mMotor.NormalCFM; }
            set { mMotor.NormalCFM = value; }
        }

        /// <summary>
        /// Gets or sets the restitution.
        /// </summary>
        /// <value>
        /// The restitution.
        /// </value>
        public float Restitution
        {
            get { return mMotor.Restitution; }
            set { mMotor.Restitution = value; }
        }

        /// <summary>
        /// Gets or sets the stop CFM.
        /// </summary>
        /// <value>
        /// The stop CFM.
        /// </value>
        public Vector3 StopCFM
        {
            get { return mMotor.StopCFM; }
            set { mMotor.StopCFM = value; }
        }

        /// <summary>
        /// Gets or sets the stop erp.
        /// </summary>
        /// <value>
        /// The stop erp.
        /// </value>
        public Vector3 StopERP
        {
            get { return mMotor.StopERP; }
            set { mMotor.StopERP = value; }
        }

        /// <summary>
        /// Gets or sets the target velocity.
        /// </summary>
        /// <value>
        /// The target velocity.
        /// </value>
        public Vector3 TargetVelocity
        {
            get { return mMotor.TargetVelocity; }
            set { mMotor.TargetVelocity = value; }
        }

        /// <summary>
        /// Gets or sets the upper limit.
        /// </summary>
        /// <value>
        /// The upper limit.
        /// </value>
        public Vector3 UpperLimit
        {
            get { return mMotor.UpperLimit; }
            set { mMotor.UpperLimit = value; }
        }
    }
}
