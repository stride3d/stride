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
    [DefaultEntityComponentProcessor(typeof(InstancingProcessor))]
    public class InstancingComponent : ActivableEntityComponent
    {
        /// <summary>
        /// Gets or sets the type of the instancing.
        /// </summary>
        /// <value>The type of the instancing.</value>
        /// <userdoc>The type of the instancing</userdoc>
        [DataMember(10)]
        [NotNull]
        [Display("Instancing Type", Expand = ExpandRule.Always)]
        public IInstancing Type { get; set; }
    }
}
