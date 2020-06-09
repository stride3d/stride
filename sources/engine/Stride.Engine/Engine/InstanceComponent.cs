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
    /// <summary>
    /// Represents a single instance of a model instancing component.
    /// </summary>
    /// <seealso cref="Stride.Engine.ActivableEntityComponent" />
    [DataContract("InstanceComponent")]
    [Display("Instance", Expand = ExpandRule.Once)]
    [ComponentCategory("Model")]
    [DefaultEntityComponentProcessor(typeof(InstanceProcessor))]
    public sealed class InstanceComponent : ActivableEntityComponent
    {
        private InstancingComponent master;

        /// <summary>
        /// Gets or sets the referenced <see cref="InstancingComponent"/> to instance.
        /// </summary>
        /// <value>The referenced <see cref="InstancingComponent"/> to instance.</value>
        /// <userdoc>The "Master" <see cref="InstancingComponent"/> to instance. If not set, it tries to find one in the parent entities</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Instancing", Expand = ExpandRule.Always)]
        public InstancingComponent Master
        {
            get => master;
            set
            {
                if (value != master)
                {
                    // Reject instancing that isn't set
                    if (value != null && value.Type == null)
                        return;

                    RemoveFromMaster();
                    master = value;
                    AddToMaster();
                }
            }
        }

        private void AddToMaster()
        {
            if (master != null && master.Type is InstancingEntityTransform instancing)
            {
                instancing.AddInstance(this);
            }
        }

        private void RemoveFromMaster()
        {
            if (master != null && master.Type is InstancingEntityTransform instancing)
            {
                instancing.RemoveInstance(this);
            }
        }
    }
}
