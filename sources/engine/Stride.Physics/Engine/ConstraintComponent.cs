// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Physics.Engine;

namespace Stride.Physics
{
    /// <summary>
    /// A component descrbing a physical constraint between two rigidbodies or a rigidbody and world.
    /// </summary>
    [DataContract("PhysicsConstraintComponent")]
    [Display("Physics Constraint")]
    [DefaultEntityComponentProcessor(typeof(ConstraintProcessor))]
    [AllowMultipleComponents]
    [ComponentOrder(3010)]
    [ComponentCategory("Physics")]
    public class ConstraintComponent : ActivableEntityComponent
    {
        internal ConstraintProcessor constraintProcessor;

        /// <summary>
        /// (Required) Rigidbody A used for body-body and body-world constraints.
        /// </summary>
        /// <userdoc>
        /// (Required) Rigidbody A used for body-body and body-world constraints.
        /// </userdoc>
        [MemberRequired]
        public RigidbodyComponent BodyA { get; set; }

        /// <summary>
        /// (Optional) Rigidbody B used for body-body constraints.
        /// </summary>
        /// <userdoc>
        /// (Optional) Rigidbody B used for body-body constraints.
        /// </userdoc>
        public RigidbodyComponent BodyB { get; set; }

        /// <summary>
        /// Description of the constraint to create.
        /// </summary>
        /// <userdoc>
        /// Description of the constraint to create.
        /// </userdoc>
        [MemberRequired]
        public IConstraintDesc Description { get; set; }

        /// <summary>
        /// When true, body A and body B will not collide with each other.
        /// </summary>
        /// <userdoc>
        /// When true, body A and body B will not collide with each other.
        /// </userdoc>
        public bool DisableCollisionsBetweenBodies { get; set; }

        /// <summary>
        /// Constructed constraint object.
        /// </summary>
        [DataMemberIgnore]
        public Constraint Constraint { get; internal set; }

        /// <summary>
        /// Simulation to which this constraint was added.
        /// </summary>
        [DataMemberIgnore]
        public Simulation Simulation { get; internal set; }

        /// <summary>
        /// Removes the currently used <see cref="Constraint"/> and recreates it.
        /// </summary>
        /// <remarks>
        /// Need to be called after modifying any of the properties.
        /// </remarks>
        public void RecreateConstraint() => constraintProcessor.Recreate(this);
    }
}
