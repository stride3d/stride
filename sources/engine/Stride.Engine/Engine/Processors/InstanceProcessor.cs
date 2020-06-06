using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceWire.TcpIp;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine.Rendering;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    public class InstanceProcessor : EntityProcessor<InstanceComponent>
    {
        public InstanceProcessor() 
            : base (typeof(TransformComponent)) // Requires TransformComponent
        {
            // After TransformProcessor but before InstancingProcessor
            Order = -110;
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InstanceComponent component, [NotNull] InstanceComponent data)
        {
            var master = component.Master;
            if (master != null && master.Type is InstancingEntityTransform instancing)
            {
                instancing.RemoveInstance(component);
            }
        }

        private InstancingComponent FindMasterInParents(Entity entity)
        {
            var parent = entity?.GetParent();
            if (parent != null)
            {
                return parent.Get<InstancingComponent>() ?? FindMasterInParents(parent);
            }

            return null;
        }
    }
}
