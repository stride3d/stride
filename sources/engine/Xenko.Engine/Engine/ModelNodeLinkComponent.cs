// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Engine.Design;
using Xenko.Engine.Processors;

namespace Xenko.Engine
{
    [DataContract("ModelNodeLinkComponent")]
    [Display("Bone link", Expand = ExpandRule.Once)]
    [DefaultEntityComponentProcessor(typeof(ModelNodeLinkProcessor))]
    [ComponentOrder(1500)]
    [ComponentCategory("Model")]
    public sealed class ModelNodeLinkComponent : EntityComponent
    {
        private ModelComponent target;

        [DataMemberIgnore]
        public bool IsValid { get; private set; }

        /// <summary>
        /// Gets or sets the model which contains the hierarchy to use.
        /// </summary>
        /// <value>
        /// The model which contains the hierarchy to use.
        /// </value>
        /// <userdoc>The model that contains the skeleton to attach this entity to. If null, the entity attaches to the parent. 
        /// Note: Xenko does not support as target entities that themself linked to another bone.</userdoc>
        [DataMember(10)]
        [Display("Model (parent if not set)")]
        public ModelComponent Target
        {
            get
            {
                return target;
            }
            set
            {
                ValidityCheck(value);
                target = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        /// <value>
        /// The name of the node.
        /// </value>
        /// <userdoc>The bone/joint to attach this entity to.</userdoc>
        [DataMember(20)]
        [Display("Bone")]
        public string NodeName { get; set; }

        public void ValidityCheck()
        {
            ValidityCheck(target);
        }

        public void ValidityCheck(ModelComponent targetToValidate)
        {
            IsValid = targetToValidate == null ||
                        Entity == null ||
                        targetToValidate.Entity == null ||
                        (targetToValidate.Entity.Id != Entity.Id
                        && RecurseCheckChildren(Entity.Transform.Children, targetToValidate.Entity.Transform)
                        && CheckParent(targetToValidate.Entity.Transform));
        }

        internal void OnHierarchyChanged(object sender, Entity entity)
        {
            if (entity == null || entity.Id != Target?.Entity.Id) return;
            ValidityCheck();
        }

        private bool CheckParent(TransformComponent targetTransform)
        {
            var parent = targetTransform.Parent;
            while (parent != null)
            {
                if (targetTransform.Entity.Id == parent.Entity.Id)
                {
                    return false;
                }

                parent = parent.Parent;
            }

            return true;
        }

        private bool RecurseCheckChildren(FastCollection<TransformComponent> children, TransformComponent targetTransform)
        {
            foreach (var transformComponentChild in children)
            {
                if (transformComponentChild.Parent == null) continue; // skip this case, the parent has not updated it's list yet

                if (!RecurseCheckChildren(transformComponentChild.Children, targetTransform))
                    return false;

                if (targetTransform.Entity.Id != transformComponentChild.Entity.Id)
                    continue;

                return false;
            }
            return true;
        }
    }
}
