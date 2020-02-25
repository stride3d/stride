// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Particles.DebugDraw;

namespace Xenko.Particles
{
    /// <summary>
    /// The <see cref="ParticleModule"/> is a base class for all plugins (initializers and updaters) used by the emitter
    /// Each plugin operates over one or several <see cref="ParticleFields"/> updating or setting up the particle state
    /// Additionally, each plugin can inherit some properties from the parent particle system, which are usually passed by the user.
    /// </summary>
    [DataContract("PaticleModule")]
    public abstract class ParticleModule : ParticleTransform
    {
        internal delegate void ChangeParticleFields(ParticleFieldDescription fieldDescription);
        internal ChangeParticleFields AddFieldDescription = null;
        internal ChangeParticleFields RemoveFieldDescription = null;

        private bool enabled = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ParticleModule"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        [DataMember(-10)]
        [DefaultValue(true)]
        public bool Enabled
        {
            get
            {
                return enabled;
            }

            set
            {
                if (enabled != value)
                {
                    if (enabled && RemoveFieldDescription != null)
                    {
                        for (var i = 0; i < RequiredFields.Count; i++)
                        {
                            RemoveFieldDescription(RequiredFields[i]);
                        }
                    }
                    else if (!enabled && AddFieldDescription != null)
                    {
                        for (var i = 0; i < RequiredFields.Count; i++)
                        {
                            AddFieldDescription(RequiredFields[i]);
                        }
                    }
                }

                enabled = value;
            }
        }

        /// <summary>
        /// Resets the current state to the module's initial state
        /// </summary>
        public virtual void ResetSimulation() { }

        /// <summary>
        /// Attepmts to get a debug shape (shape type and location matrix) for the current module in order to display its boundaries better
        /// </summary>
        /// <param name="debugDrawShape">Type of the debug draw shape</param>
        /// <param name="translation">Translation of the shape</param>
        /// <param name="rotation">Rotation of the shape</param>
        /// <param name="scale">Scaling of the shape</param>
        /// <returns></returns>
        public virtual bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            debugDrawShape = DebugDrawShape.None;
            scale = Vector3.One;
            translation = Vector3.Zero;
            rotation = Quaternion.Identity;
            return false;
        }

        /// <summary>
        /// A list of fields required by the module to operate properly.
        /// Please fill it during construction time.
        /// </summary>
        [DataMemberIgnore]
        public List<ParticleFieldDescription> RequiredFields = new List<ParticleFieldDescription>(ParticlePool.DefaultMaxFielsPerPool);

        /// <summary>
        /// Sets the parent (particle system's) translation, rotation and scale (uniform)
        /// The module can choose to inherit, use or ignore any of the elements
        /// </summary>
        /// <param name="transform"><see cref="ParticleSystem"/>'s transform (from the Transform component) or identity if local space is used</param>
        /// <param name="parent">The parent <see cref="ParticleSystem"/></param>
        public virtual void SetParentTRS(ParticleTransform transform, ParticleSystem parent)
        {
            SetParentTransform(transform);
        }

        /// <summary>
        /// Invalidates relation of this emitter to any other emitters that might be referenced
        /// </summary>
        public virtual void InvalidateRelations()
        {
            
        }

        /// <inheritdoc />
        public virtual void PreUpdate() { }
    }
}
