// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Physics
{
    public class SliderConstraint : Constraint
    {
        internal BulletSharp.SliderConstraint InternalSliderConstraint;

        /// <summary>
        /// Gets or sets the upper linear limit.
        /// </summary>
        /// <value>
        /// The upper linear limit.
        /// </value>
        public float UpperLinearLimit
        {
            get { return InternalSliderConstraint.UpperLinearLimit; }
            set { InternalSliderConstraint.UpperLinearLimit = value; }
        }

        /// <summary>
        /// Gets or sets the lower linear limit.
        /// </summary>
        /// <value>
        /// The lower linear limit.
        /// </value>
        public float LowerLinearLimit
        {
            get { return InternalSliderConstraint.LowerLinearLimit; }
            set { InternalSliderConstraint.LowerLinearLimit = value; }
        }

        /// <summary>
        /// Gets or sets the upper angular limit.
        /// </summary>
        /// <value>
        /// The upper angular limit.
        /// </value>
        public float UpperAngularLimit
        {
            get { return InternalSliderConstraint.UpperAngularLimit; }
            set { InternalSliderConstraint.UpperAngularLimit = value; }
        }

        /// <summary>
        /// Gets or sets the lower angular limit.
        /// </summary>
        /// <value>
        /// The lower angular limit.
        /// </value>
        public float LowerAngularLimit
        {
            get { return InternalSliderConstraint.LowerAngularLimit; }
            set { InternalSliderConstraint.LowerAngularLimit = value; }
        }

        /// <summary>
        /// Gets the angular depth.
        /// </summary>
        /// <value>
        /// The angular depth.
        /// </value>
        public float AngularDepth
        {
            get { return InternalSliderConstraint.AngularDepth; }
        }

        /// <summary>
        /// Gets the angular position.
        /// </summary>
        /// <value>
        /// The angular position.
        /// </value>
        public float AngularPosition
        {
            get { return InternalSliderConstraint.AngularPosition; }
        }

        /// <summary>
        /// Gets or sets the damping dir angular.
        /// </summary>
        /// <value>
        /// The damping dir angular.
        /// </value>
        public float DampingDirAngular
        {
            get { return InternalSliderConstraint.DampingDirAngular; }
            set { InternalSliderConstraint.DampingDirAngular = value; }
        }

        /// <summary>
        /// Gets or sets the damping dir linear.
        /// </summary>
        /// <value>
        /// The damping dir linear.
        /// </value>
        public float DampingDirLinear
        {
            get { return InternalSliderConstraint.DampingDirLinear; }
            set { InternalSliderConstraint.DampingDirLinear = value; }
        }

        /// <summary>
        /// Gets or sets the damping lim angular.
        /// </summary>
        /// <value>
        /// The damping lim angular.
        /// </value>
        public float DampingLimAngular
        {
            get { return InternalSliderConstraint.DampingLimAngular; }
            set { InternalSliderConstraint.DampingLimAngular = value; }
        }

        /// <summary>
        /// Gets or sets the damping lim linear.
        /// </summary>
        /// <value>
        /// The damping lim linear.
        /// </value>
        public float DampingLimLinear
        {
            get { return InternalSliderConstraint.DampingLimLinear; }
            set { InternalSliderConstraint.DampingLimLinear = value; }
        }

        /// <summary>
        /// Gets or sets the damping ortho angular.
        /// </summary>
        /// <value>
        /// The damping ortho angular.
        /// </value>
        public float DampingOrthoAngular
        {
            get { return InternalSliderConstraint.DampingOrthoAngular; }
            set { InternalSliderConstraint.DampingOrthoAngular = value; }
        }

        /// <summary>
        /// Gets or sets the damping ortho linear.
        /// </summary>
        /// <value>
        /// The damping ortho linear.
        /// </value>
        public float DampingOrthoLinear
        {
            get { return InternalSliderConstraint.DampingOrthoLinear; }
            set { InternalSliderConstraint.DampingOrthoLinear = value; }
        }

        /// <summary>
        /// Gets the linear depth.
        /// </summary>
        /// <value>
        /// The linear depth.
        /// </value>
        public float LinearDepth
        {
            get { return InternalSliderConstraint.LinearDepth; }
        }

        /// <summary>
        /// Gets the linear position.
        /// </summary>
        /// <value>
        /// The linear position.
        /// </value>
        public float LinearPosition
        {
            get { return InternalSliderConstraint.LinearPosition; }
        }

        /// <summary>
        /// Gets or sets the maximum ang motor force.
        /// </summary>
        /// <value>
        /// The maximum ang motor force.
        /// </value>
        public float MaxAngMotorForce
        {
            get { return InternalSliderConstraint.MaxAngMotorForce; }
            set { InternalSliderConstraint.MaxAngMotorForce = value; }
        }

        /// <summary>
        /// Gets or sets the maximum linear motor force.
        /// </summary>
        /// <value>
        /// The maximum linear motor force.
        /// </value>
        public float MaxLinearMotorForce
        {
            get { return InternalSliderConstraint.MaxLinearMotorForce; }
            set { InternalSliderConstraint.MaxLinearMotorForce = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [powered angular motor].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [powered angular motor]; otherwise, <c>false</c>.
        /// </value>
        public bool PoweredAngularMotor
        {
            get { return InternalSliderConstraint.PoweredAngularMotor; }
            set { InternalSliderConstraint.PoweredAngularMotor = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [powered linear motor].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [powered linear motor]; otherwise, <c>false</c>.
        /// </value>
        public bool PoweredLinearMotor
        {
            get { return InternalSliderConstraint.PoweredLinearMotor; }
            set { InternalSliderConstraint.PoweredLinearMotor = value; }
        }

        /// <summary>
        /// Gets or sets the restitution dir angular.
        /// </summary>
        /// <value>
        /// The restitution dir angular.
        /// </value>
        public float RestitutionDirAngular
        {
            get { return InternalSliderConstraint.RestitutionDirAngular; }
            set { InternalSliderConstraint.RestitutionDirAngular = value; }
        }

        /// <summary>
        /// Gets or sets the restitution dir linear.
        /// </summary>
        /// <value>
        /// The restitution dir linear.
        /// </value>
        public float RestitutionDirLinear
        {
            get { return InternalSliderConstraint.RestitutionDirLinear; }
            set { InternalSliderConstraint.RestitutionDirLinear = value; }
        }

        /// <summary>
        /// Gets or sets the restitution lim angular.
        /// </summary>
        /// <value>
        /// The restitution lim angular.
        /// </value>
        public float RestitutionLimAngular
        {
            get { return InternalSliderConstraint.RestitutionLimAngular; }
            set { InternalSliderConstraint.RestitutionLimAngular = value; }
        }

        /// <summary>
        /// Gets or sets the restitution lim linear.
        /// </summary>
        /// <value>
        /// The restitution lim linear.
        /// </value>
        public float RestitutionLimLinear
        {
            get { return InternalSliderConstraint.RestitutionLimLinear; }
            set { InternalSliderConstraint.RestitutionLimLinear = value; }
        }

        /// <summary>
        /// Gets or sets the restitution ortho angular.
        /// </summary>
        /// <value>
        /// The restitution ortho angular.
        /// </value>
        public float RestitutionOrthoAngular
        {
            get { return InternalSliderConstraint.RestitutionOrthoAngular; }
            set { InternalSliderConstraint.RestitutionOrthoAngular = value; }
        }

        /// <summary>
        /// Gets or sets the restitution ortho linear.
        /// </summary>
        /// <value>
        /// The restitution ortho linear.
        /// </value>
        public float RestitutionOrthoLinear
        {
            get { return InternalSliderConstraint.RestitutionOrthoLinear; }
            set { InternalSliderConstraint.RestitutionOrthoLinear = value; }
        }

        /// <summary>
        /// Gets or sets the softness dir angular.
        /// </summary>
        /// <value>
        /// The softness dir angular.
        /// </value>
        public float SoftnessDirAngular
        {
            get { return InternalSliderConstraint.SoftnessDirAngular; }
            set { InternalSliderConstraint.SoftnessDirAngular = value; }
        }

        /// <summary>
        /// Gets or sets the softness dir linear.
        /// </summary>
        /// <value>
        /// The softness dir linear.
        /// </value>
        public float SoftnessDirLinear
        {
            get { return InternalSliderConstraint.SoftnessDirLinear; }
            set { InternalSliderConstraint.SoftnessDirLinear = value; }
        }

        /// <summary>
        /// Gets or sets the softness lim angular.
        /// </summary>
        /// <value>
        /// The softness lim angular.
        /// </value>
        public float SoftnessLimAngular
        {
            get { return InternalSliderConstraint.SoftnessLimAngular; }
            set { InternalSliderConstraint.SoftnessLimAngular = value; }
        }

        /// <summary>
        /// Gets or sets the softness lim linear.
        /// </summary>
        /// <value>
        /// The softness lim linear.
        /// </value>
        public float SoftnessLimLinear
        {
            get { return InternalSliderConstraint.SoftnessLimLinear; }
            set { InternalSliderConstraint.SoftnessLimLinear = value; }
        }

        /// <summary>
        /// Gets or sets the softness ortho angular.
        /// </summary>
        /// <value>
        /// The softness ortho angular.
        /// </value>
        public float SoftnessOrthoAngular
        {
            get { return InternalSliderConstraint.SoftnessOrthoAngular; }
            set { InternalSliderConstraint.SoftnessOrthoAngular = value; }
        }

        /// <summary>
        /// Gets or sets the softness ortho linear.
        /// </summary>
        /// <value>
        /// The softness ortho linear.
        /// </value>
        public float SoftnessOrthoLinear
        {
            get { return InternalSliderConstraint.SoftnessOrthoLinear; }
            set { InternalSliderConstraint.SoftnessOrthoLinear = value; }
        }

        /// <summary>
        /// Gets a value indicating whether [solve angular limit].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [solve angular limit]; otherwise, <c>false</c>.
        /// </value>
        public bool SolveAngularLimit
        {
            get { return InternalSliderConstraint.SolveAngularLimit; }
        }

        /// <summary>
        /// Gets a value indicating whether [solve linear limit].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [solve linear limit]; otherwise, <c>false</c>.
        /// </value>
        public bool SolveLinearLimit
        {
            get { return InternalSliderConstraint.SolveLinearLimit; }
        }

        /// <summary>
        /// Gets or sets the target angular motor velocity.
        /// </summary>
        /// <value>
        /// The target angular motor velocity.
        /// </value>
        public float TargetAngularMotorVelocity
        {
            get { return InternalSliderConstraint.TargetAngularMotorVelocity; }
            set { InternalSliderConstraint.TargetAngularMotorVelocity = value; }
        }

        /// <summary>
        /// Gets or sets the target linear motor velocity.
        /// </summary>
        /// <value>
        /// The target linear motor velocity.
        /// </value>
        public float TargetLinearMotorVelocity
        {
            get { return InternalSliderConstraint.TargetLinearMotorVelocity; }
            set { InternalSliderConstraint.TargetLinearMotorVelocity = value; }
        }

        /// <summary>
        /// Sets the frames.
        /// </summary>
        /// <param name="frameA">The frame a.</param>
        /// <param name="frameB">The frame b.</param>
        public void SetFrames(Matrix frameA, Matrix frameB)
        {
            InternalSliderConstraint.SetFrames(frameA, frameB);
        }
    }
}
