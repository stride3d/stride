// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Physics
{
    public class Generic6DoFConstraint : Constraint
    {
        private BulletSharp.Generic6DofConstraint mInternalGeneric6DofConstraint;
        internal BulletSharp.Generic6DofConstraint InternalGeneric6DofConstraint
        {
            get
            {
                return mInternalGeneric6DofConstraint;
            }
            set
            {
                mInternalGeneric6DofConstraint = value;

                //fill translational motor
                TranslationalLimitMotor = new TranslationalLimitMotor(mInternalGeneric6DofConstraint.TranslationalLimitMotor);

                //fill rotational motors
                for (var i = 0; i < 3; i++)
                {
                    RotationalLimitMotor[i] = new RotationalLimitMotor(mInternalGeneric6DofConstraint.GetRotationalLimitMotor(i));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Generic6DoFConstraint"/> class.
        /// </summary>
        public Generic6DoFConstraint()
        {
            RotationalLimitMotor = new RotationalLimitMotor[3];
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (InternalConstraint == null) return;

            for (var i = 0; i < 3; i++)
            {
                if (RotationalLimitMotor[i] == null) continue;
                RotationalLimitMotor[i].Dispose();
                RotationalLimitMotor[i] = null;
            }

            if (TranslationalLimitMotor != null)
            {
                TranslationalLimitMotor.Dispose();
                TranslationalLimitMotor = null;
            }

            base.Dispose();
        }

        /// <summary>
        /// Gets or sets the angular lower limit.
        /// </summary>
        /// <value>
        /// The angular lower limit.
        /// </value>
        public Vector3 AngularLowerLimit
        {
            get { return InternalGeneric6DofConstraint.AngularLowerLimit; }
            set { InternalGeneric6DofConstraint.AngularLowerLimit = value; }
        }

        /// <summary>
        /// Gets or sets the angular upper limit.
        /// </summary>
        /// <value>
        /// The angular upper limit.
        /// </value>
        public Vector3 AngularUpperLimit
        {
            get { return InternalGeneric6DofConstraint.AngularUpperLimit; }
            set { InternalGeneric6DofConstraint.AngularUpperLimit = value; }
        }

        /// <summary>
        /// Gets or sets the linear lower limit.
        /// </summary>
        /// <value>
        /// The linear lower limit.
        /// </value>
        public Vector3 LinearLowerLimit
        {
            get { return InternalGeneric6DofConstraint.LinearLowerLimit; }
            set { InternalGeneric6DofConstraint.LinearLowerLimit = value; }
        }

        /// <summary>
        /// Gets or sets the linear upper limit.
        /// </summary>
        /// <value>
        /// The linear upper limit.
        /// </value>
        public Vector3 LinearUpperLimit
        {
            get { return InternalGeneric6DofConstraint.LinearUpperLimit; }
            set { InternalGeneric6DofConstraint.LinearUpperLimit = value; }
        }

        /// <summary>
        /// Sets the frames.
        /// </summary>
        /// <param name="frameA">The frame a.</param>
        /// <param name="frameB">The frame b.</param>
        public void SetFrames(Matrix frameA, Matrix frameB)
        {
            InternalGeneric6DofConstraint.SetFrames(frameA, frameB);
        }

        /// <summary>
        /// Sets the limit.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        public void SetLimit(int axis, float lo, float hi)
        {
            InternalGeneric6DofConstraint.SetLimit(axis, lo, hi);
        }

        /// <summary>
        /// Gets the translational limit motor.
        /// </summary>
        /// <value>
        /// The translational limit motor.
        /// </value>
        public TranslationalLimitMotor TranslationalLimitMotor { get; private set; }

        /// <summary>
        /// Gets the rotational limit motor.
        /// </summary>
        /// <value>
        /// The rotational limit motor.
        /// </value>
        public RotationalLimitMotor[] RotationalLimitMotor { get; private set; }
    }
}
