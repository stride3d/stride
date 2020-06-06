using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine.Design;
using Stride.Engine.Processors;
using Stride.Graphics;

namespace Stride.Engine
{
    [DataContract("InstancingEntityTransform")]
    [Display("EntityTransform")]
    public class InstancingEntityTransform : InstancingUserArray
    {
        /// <summary>
        /// Gets or sets the referenced <see cref="ModelComponent"/> to instance.
        /// </summary>
        /// <value>The referenced <see cref="ModelComponent"/> to instance.</value>
        /// <userdoc>The "Master" <see cref="ModelComponent"/> to instance.</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Master Model", Expand = ExpandRule.Always)]
        public ModelComponent Master { get; set; }

        public override void Update()
        {
            base.Update();
        }

        List<Entity> instanceEntities = new List<Entity>();

        internal Entity GetInstanceEntity(int instanceId)
        {
            return instanceEntities[instanceId];
        }

        internal void ClearEntities()
        {        
            instanceEntities.Clear();
        }

        internal void AddInstanceEntity(Entity entity)
        {
            instanceEntities.Add(entity);
        }
    }
}
