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
    [DataContract]
    public abstract class InstancingManyBase : IInstancingMany
    {
        /// <summary>
        /// The instance count
        /// </summary>
        [DataMemberIgnore]
        public virtual int InstanceCount { get; set; }

        /// <summary>
        /// The bounding box of the world matrices, updated automatically by the <see cref="InstancingProcessor"/>.
        /// </summary>
        [DataMemberIgnore]
        public virtual BoundingBox BoundingBox { get; set; } = BoundingBox.Empty;

        public virtual void Update()
        {
            
        }
    }
}
