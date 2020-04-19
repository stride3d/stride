// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Physics
{
    public class Point2PointConstraint : Constraint
    {
        /// <summary>
        /// Gets or sets the pivot in a.
        /// </summary>
        /// <value>
        /// The pivot in a.
        /// </value>
        public Vector3 PivotInA 
        {
            get { return InternalPoint2PointConstraint.PivotInA; }
            set { InternalPoint2PointConstraint.PivotInA = value; } 
        }

        /// <summary>
        /// Gets or sets the pivot in b.
        /// </summary>
        /// <value>
        /// The pivot in b.
        /// </value>
        public Vector3 PivotInB
        {
            get { return InternalPoint2PointConstraint.PivotInB; }
            set { InternalPoint2PointConstraint.PivotInB = value; }
        }

        /// <summary>
        /// Gets or sets the damping.
        /// </summary>
        /// <value>
        /// The damping.
        /// </value>
        public float Damping
        {
            get { return InternalPoint2PointConstraint.Setting.Damping; }
            set { InternalPoint2PointConstraint.Setting.Damping = value; }
        }

        /// <summary>
        /// Gets or sets the impulse clamp.
        /// </summary>
        /// <value>
        /// The impulse clamp.
        /// </value>
        public float ImpulseClamp
        {
            get { return InternalPoint2PointConstraint.Setting.ImpulseClamp; }
            set { InternalPoint2PointConstraint.Setting.ImpulseClamp = value; }
        }

        /// <summary>
        /// Gets or sets the tau.
        /// </summary>
        /// <value>
        /// The tau.
        /// </value>
        public float Tau
        {
            get { return InternalPoint2PointConstraint.Setting.Tau; }
            set { InternalPoint2PointConstraint.Setting.Tau = value; }
        }

        internal BulletSharp.Point2PointConstraint InternalPoint2PointConstraint;
    }
}
