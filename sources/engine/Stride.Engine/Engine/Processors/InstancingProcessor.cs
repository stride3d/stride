using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Annotations;
using Stride.Core.Threading;
using Stride.Games;
using Stride.Rendering;

namespace Stride.Engine.Processors
{
    public class InstancingProcessor : EntityProcessor<InstancingComponent, InstancingProcessor.InstancingData>
    {
        public class InstancingData
        {
            public ModelComponent ModelComponent;
        }

        public InstancingProcessor() 
            : base (typeof(ModelComponent)) // Requires a ModelComponent
        {

        }

        public override void Draw(RenderContext context)
        {
            Dispatcher.ForEach(ComponentDatas, entity =>
            {
                var instancingComponent = entity.Key;
                var instancingData = entity.Value;

                UpdateInstancingData(instancingComponent, instancingData);
            });
        }

        private void UpdateInstancingData(InstancingComponent instancingComponent, InstancingData instancingData)
        {
            if (instancingComponent.Enabled && instancingComponent.InstanceCount > 0)
            {
                instancingComponent.Process();
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData data)
        {
            base.OnEntityComponentAdding(entity, component, data);
        }

        // Instancing data per InstancingComponent
        protected override InstancingData GenerateComponentData([NotNull] Entity entity, [NotNull] InstancingComponent component)
        {
            return new InstancingData();
        }

        protected override bool IsAssociatedDataValid([NotNull] Entity entity, [NotNull] InstancingComponent component, [NotNull] InstancingData associatedData)
        {
            return true;
        }
    }
}
