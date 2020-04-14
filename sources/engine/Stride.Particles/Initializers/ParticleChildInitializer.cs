// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Particles.Spawners;

namespace Stride.Particles.Initializers
{
    /// <summary>
    /// Base class for initializers which reference a parent particle emitter
    /// </summary>
    [DataContract("ParticleChildInitializer")]
    public abstract class ParticleChildInitializer : ParticleInitializer
    {
        /// <summary>
        /// Referenced parent emitter
        /// </summary>
        [DataMemberIgnore]
        protected ParticleEmitter Parent;

        /// <summary>
        /// Referenced parent emitter's name
        /// </summary>
        [DataMemberIgnore]
        private string parentName;

        /// <summary>
        /// <c>true</c> is the parent's name has changed or the particle system has been invalidated
        /// </summary>
        [DataMemberIgnore]
        private bool isParentNameDirty = true;

        /// <summary>
        /// Name by which to reference a followed (parent) emitter
        /// </summary>
        /// <userdoc>
        /// Name by which to reference a followed (parent) emitter
        /// </userdoc>
        [DataMember(11)]
        [Display("Parent emitter")]
        public string ParentName
        {
            get { return parentName; }
            set
            {
                parentName = value;
                isParentNameDirty = true;
            }
        }

        /// <summary>
        /// The parent seed offset is used to determine which particle from the pool should be picked as a parent in case there is no control group
        /// </summary>
        /// <userdoc>
        /// The parent seed offset is used to determine which particle from the pool should be picked as a parent in case there is no control group
        /// </userdoc>
        [DataMember(12)]
        [Display("Parent Offset")]
        public uint ParentSeedOffset { get; set; } = 0;

        /// <summary>
        /// Some initializers require fine control between parent and child emitters. Use the control group to assign such meta-fields.
        /// </summary>
        [DataMember(13)]
        [Display("Spawn Control Group")]
        public ParentControlFlag ParentControlFlag { get; set; } = ParentControlFlag.Group00;

        /// <summary>
        /// Gets a field accessor to the parent emitter's spawn control field, if it exists
        /// </summary>
        /// <returns></returns>
        protected ParticleFieldAccessor<ParticleChildrenAttribute> GetSpawnControlField()
        {
            var groupIndex = (int)ParentControlFlag;
            if (groupIndex >= ParticleFields.ChildrenFlags.Length)
                return ParticleFieldAccessor<ParticleChildrenAttribute>.Invalid();

            return Parent?.Pool?.GetField(ParticleFields.ChildrenFlags[groupIndex]) ?? ParticleFieldAccessor<ParticleChildrenAttribute>.Invalid();
        } 

        /// <inheritdoc />
        public override void SetParentTRS(ParticleTransform transform, ParticleSystem parentSystem)
        {
            base.SetParentTRS(transform, parentSystem);

            if (isParentNameDirty)
            {
                RemoveControlGroup();

                Parent = parentSystem?.GetEmitterByName(ParentName);

                AddControlGroup();

                isParentNameDirty = false;
            }
        }

        /// <inheritdoc />
        public override void InvalidateRelations()
        {
            base.InvalidateRelations();

            RemoveControlGroup();

            Parent = null;
            isParentNameDirty = true;
        }

        /// <summary>
        /// Removes the old required control group field from the parent emitter's pool
        /// </summary>
        protected virtual void RemoveControlGroup()
        {
            // Override this method to remove required fields from the parent
        }

        /// <summary>
        /// Adds the required control group field to the parent emitter's pool
        /// </summary>
        protected virtual void AddControlGroup()
        {
            // Override this method to add required fields to the parent
        }
    }
}

