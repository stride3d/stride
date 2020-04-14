// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Xenko.Physics
{
    public class Constraint : IDisposable, IRelative
    {
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (InternalConstraint == null) return;

            InternalConstraint.Dispose();
            InternalConstraint = null;

            if (RigidBodyA != null && RigidBodyA.LinkedConstraints.Contains(this))
            {
                RigidBodyA.LinkedConstraints.Remove(this);
            }

            if (RigidBodyB != null && RigidBodyB.LinkedConstraints.Contains(this))
            {
                RigidBodyB.LinkedConstraints.Remove(this);
            }
        }

        /// <summary>
        /// Gets the rigid body a.
        /// </summary>
        /// <value>
        /// The rigid body a.
        /// </value>
        public RigidbodyComponent RigidBodyA { get; internal set; }
        /// <summary>
        /// Gets the rigid body b.
        /// </summary>
        /// <value>
        /// The rigid body b.
        /// </value>
        public RigidbodyComponent RigidBodyB { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Constraint"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            get { return InternalConstraint.IsEnabled; }
            set { InternalConstraint.IsEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the breaking impulse threshold.
        /// </summary>
        /// <value>
        /// The breaking impulse threshold.
        /// </value>
        public float BreakingImpulseThreshold
        {
            get { return InternalConstraint.BreakingImpulseThreshold; }
            set { InternalConstraint.BreakingImpulseThreshold = value; }
        }

        private bool feedbackEnabled;

        /// <summary>
        /// Gets the applied impulse.
        /// </summary>
        /// <value>
        /// The applied impulse.
        /// </value>
        public float AppliedImpulse
        {
            get
            {
                if (feedbackEnabled) return InternalConstraint.AppliedImpulse;
                InternalConstraint.EnableFeedback(true);
                feedbackEnabled = true;
                return InternalConstraint.AppliedImpulse;
            }
        }

        internal BulletSharp.TypedConstraint InternalConstraint;

        /// <summary>
        /// Gets the Simulation where this Constraint is being processed
        /// </summary>
        public Simulation Simulation { get; internal set; }
    }
}
