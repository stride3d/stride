// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Physics
{
    public class RotationalLimitMotor
    {
        private BulletSharp.RotationalLimitMotor mMotor;

        /// <summary>
        /// Initializes a new instance of the <see cref="RotationalLimitMotor"/> class.
        /// </summary>
        /// <param name="motor">The motor.</param>
        public RotationalLimitMotor(BulletSharp.RotationalLimitMotor motor)
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
        public float AccumulatedImpulse
        {
            get { return mMotor.AccumulatedImpulse; }
            set { mMotor.AccumulatedImpulse = value; }
        }

        /// <summary>
        /// Gets or sets the bounce.
        /// </summary>
        /// <value>
        /// The bounce.
        /// </value>
        public float Bounce
        {
            get { return mMotor.Bounce; }
            set { mMotor.Bounce = value; }
        }

        /// <summary>
        /// Gets or sets the current limit.
        /// </summary>
        /// <value>
        /// The current limit.
        /// </value>
        public int CurrentLimit
        {
            get { return mMotor.CurrentLimit; }
            set { mMotor.CurrentLimit = value; }
        }

        /// <summary>
        /// Gets or sets the current limit error.
        /// </summary>
        /// <value>
        /// The current limit error.
        /// </value>
        public float CurrentLimitError
        {
            get { return mMotor.CurrentLimitError; }
            set { mMotor.CurrentLimitError = value; }
        }

        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        /// <value>
        /// The current position.
        /// </value>
        public float CurrentPosition
        {
            get { return mMotor.CurrentPosition; }
            set { mMotor.CurrentPosition = value; }
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
        /// Gets or sets a value indicating whether to enable the motor.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the motor is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool EnableMotor
        {
            get { return mMotor.EnableMotor; }
            set { mMotor.EnableMotor = value; }
        }

        /// <summary>
        /// Gets or sets the hi limit.
        /// </summary>
        /// <value>
        /// The hi limit.
        /// </value>
        public float HiLimit
        {
            get { return mMotor.HiLimit; }
            set { mMotor.HiLimit = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is limited.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is limited; otherwise, <c>false</c>.
        /// </value>
        public bool IsLimited
        {
            get { return mMotor.IsLimited; }
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
        /// Gets or sets the lo limit.
        /// </summary>
        /// <value>
        /// The lo limit.
        /// </value>
        public float LoLimit
        {
            get { return mMotor.LoLimit; }
            set { mMotor.LoLimit = value; }
        }

        /// <summary>
        /// Gets or sets the maximum limit force.
        /// </summary>
        /// <value>
        /// The maximum limit force.
        /// </value>
        public float MaxLimitForce
        {
            get { return mMotor.MaxLimitForce; }
            set { mMotor.MaxLimitForce = value; }
        }

        /// <summary>
        /// Gets or sets the maximum motor force.
        /// </summary>
        /// <value>
        /// The maximum motor force.
        /// </value>
        public float MaxMotorForce
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
        public float NormalCfm
        {
            get { return mMotor.NormalCfm; }
            set { mMotor.NormalCfm = value; }
        }

        /// <summary>
        /// Gets or sets the stop CFM.
        /// </summary>
        /// <value>
        /// The stop CFM.
        /// </value>
        public float StopCfm
        {
            get { return mMotor.StopCfm; }
            set { mMotor.StopCfm = value; }
        }

        /// <summary>
        /// Gets or sets the stop erp.
        /// </summary>
        /// <value>
        /// The stop erp.
        /// </value>
        public float StopErp
        {
            get { return mMotor.StopErp; }
            set { mMotor.StopErp = value; }
        }

        /// <summary>
        /// Gets or sets the target velocity.
        /// </summary>
        /// <value>
        /// The target velocity.
        /// </value>
        public float TargetVelocity
        {
            get { return mMotor.TargetVelocity; }
            set { mMotor.TargetVelocity = value; }
        }
    }
}
